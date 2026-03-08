using Market.Web.Core.Exceptions;
using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;
using Market.Web.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Market.Web.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProfileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<UserProfile?> GetByUserIdAsync(string userId)
    {
        return await _unitOfWork.Profiles.GetByUserIdAsync(userId);
    }

    public async Task<bool> HasCompleteBasicProfileReadOnlyAsync(string userId)
    {
        return await _unitOfWork.Profiles.HasCompleteBasicProfileReadOnlyAsync(userId);
    }

    public async Task<bool> HasIbanInProfileReadOnlyAsync(string userId)
    {
        return await _unitOfWork.Profiles.HasIbanInProfileReadOnlyAsync(userId);
    }
    public async Task<EditProfileViewModel> GetEditProfileViewModelAsync(string userId, string email)
    {
        var userProfile = await _unitOfWork.Profiles.GetByUserIdAsync(userId);

        return new EditProfileViewModel
        {
            Email = email,
            FirstName = userProfile?.FirstName ?? "",
            LastName = userProfile?.LastName ?? "",
            PrivateIBAN = userProfile?.PrivateIBAN,
            ShippingAddress = userProfile?.ShippingAddress ?? new Address(),
            
            HasCompanyProfile = userProfile?.CompanyProfile != null,
            CompanyName = userProfile?.CompanyProfile?.CompanyName,
            NIP = userProfile?.CompanyProfile?.NIP,
            CompanyIBAN = userProfile?.CompanyProfile?.CompanyIBAN,
            InvoiceAddress = userProfile?.CompanyProfile?.InvoiceAddress ?? new Address()
        };
    }

    public async Task UpdateProfileAsync(string userId, EditProfileViewModel model)
    {
        var userProfile = await _unitOfWork.Profiles.GetByUserIdAsync(userId);

        if (userProfile == null)
        {
            userProfile = new UserProfile { UserId = userId };
            await _unitOfWork.Profiles.AddAsync(userProfile);
        }

        userProfile.FirstName = model.FirstName;
        userProfile.LastName = model.LastName;
        userProfile.PrivateIBAN = model.PrivateIBAN;
        userProfile.ShippingAddress = model.ShippingAddress;

        if (model.HasCompanyProfile)
        {
            if (userProfile.CompanyProfile == null)
            {
                userProfile.CompanyProfile = new CompanyProfile();
            }

            userProfile.CompanyProfile.CompanyName = model.CompanyName!;
            userProfile.CompanyProfile.NIP = model.NIP!;
            userProfile.CompanyProfile.CompanyIBAN = model.CompanyIBAN;
            userProfile.CompanyProfile.InvoiceAddress = model.InvoiceAddress;
        }
        else
        {
            if (userProfile.CompanyProfile != null)
            {
                _unitOfWork.Profiles.RemoveCompanyProfile(userProfile.CompanyProfile);
                userProfile.CompanyProfile = null;
            }
        }

        await _unitOfWork.CompleteAsync();
    }

    public async Task<MyFinancesViewModel> GetFinancesViewModelAsync(string userId)
    {
        var userProfile = await _unitOfWork.Profiles.GetByUserIdAsync(userId);
        var sales = await _unitOfWork.Orders.GetSellerSalesAsync(userId);

        decimal pending = sales
            .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped)
            .Sum(o => o.TotalPrice);

        return new MyFinancesViewModel
        {
            IBAN = userProfile?.PrivateIBAN ?? "",
            AvailableFunds = userProfile?.WalletBalance ?? 0,
            PendingFunds = pending,
            
            Transactions = sales.Select(s => new FinanceTransactionDto
            {
                OrderId = s.Id,
                Date = s.OrderDate,
                Title = s.Auction?.Title ?? "Usunięta aukcja",
                Amount = s.TotalPrice,
                Status = s.Status,
                BuyerName = s.Buyer?.UserName ?? "Nieznany"
            }).ToList()
        };
    }

    public async Task<(bool Success, string Message, decimal Amount)> WithdrawFundsAsync(string userId)
    {
        var userProfile = await _unitOfWork.Profiles.GetByUserIdAsync(userId);
        if (userProfile == null) return (false, "Profil nie istnieje.", 0);

        if (string.IsNullOrEmpty(userProfile.PrivateIBAN))
        {
            return (false, "Brak numeru konta IBAN. Uzupełnij profil.", 0);
        }

        if (userProfile.WalletBalance <= 0)
        {
            return (false, "Brak środków do wypłaty.", 0);
        }

        decimal amountToWithdraw = userProfile.WalletBalance;
        userProfile.WalletBalance = 0;

        try
        {
            await _unitOfWork.CompleteAsync();
            return (true, "Wypłacono", amountToWithdraw);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(
                "Operacja nie powiodła się z powodu konfliktu. Spróbuj ponownie.", ex);
        
        }
    }
}