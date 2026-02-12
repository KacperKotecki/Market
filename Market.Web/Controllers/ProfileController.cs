using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;
using Market.Web.Repositories;

namespace Market.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProfileRepository _profileRepository;
        private readonly IOrderRepository _orderRepository;

        public ProfileController(
            UserManager<ApplicationUser> userManager, 
            IProfileRepository profileRepository,
            IOrderRepository orderRepository)
        {
            _userManager = userManager;
            _profileRepository = profileRepository;
            _orderRepository = orderRepository;
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var userProfile = await _profileRepository.GetByUserIdAsync(user.Id);

            var model = new EditProfileViewModel
            {
                Email = user.Email,
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

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            
            ModelState.Remove("Email"); 

            if (!model.HasCompanyProfile)
            {
                ModelState.Remove("CompanyName");
                ModelState.Remove("NIP");
                ModelState.Remove("InvoiceAddress.Street");
                ModelState.Remove("InvoiceAddress.City");
                ModelState.Remove("InvoiceAddress.PostalCode");
                ModelState.Remove("InvoiceAddress.Country");
            }

            if (!ModelState.IsValid)
            {
                model.Email = user.Email; 
                return View(model);
            }

            var userProfile = await _profileRepository.GetByUserIdAsync(user.Id);

            if (userProfile == null)
            {
                userProfile = new UserProfile { UserId = user.Id };
                await _profileRepository.AddAsync(userProfile);
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
                    _profileRepository.RemoveCompanyProfile(userProfile.CompanyProfile);
                    userProfile.CompanyProfile = null;
                }
            }

            await _profileRepository.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profil został zaktualizowany.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> MyFinances()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userProfile = await _profileRepository.GetByUserIdAsync(user.Id);

            var sales = await _orderRepository.GetSellerSalesAsync(user.Id);

            decimal pending = sales
                .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped)
                .Sum(o => o.TotalPrice);

            var model = new MyFinancesViewModel
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

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawFunds()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            
            var userProfile = await _profileRepository.GetByUserIdAsync(user.Id);

            if (userProfile == null) return NotFound();

            if (string.IsNullOrEmpty(userProfile.PrivateIBAN))
            {
                TempData["Error"] = "Brak numeru konta IBAN. Uzupełnij profil.";
                return RedirectToAction(nameof(MyFinances));
            }

            if (userProfile.WalletBalance <= 0)
            {
                TempData["Error"] = "Brak środków do wypłaty.";
                return RedirectToAction(nameof(MyFinances));
            }

            decimal amountToWithdraw = userProfile.WalletBalance;
            
            userProfile.WalletBalance = 0;
            
            await _profileRepository.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Zlecono wypłatę {amountToWithdraw:N2} PLN na konto {userProfile.PrivateIBAN}.";
            return RedirectToAction(nameof(MyFinances));
        }
    }
}