using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;

namespace Market.Web.Services;

public interface IProfileService
{
    Task<UserProfile?> GetByUserIdAsync(string userId);
    Task<bool> HasCompleteBasicProfileReadOnlyAsync(string userId);
    Task<bool> HasIbanInProfileReadOnlyAsync(string userId);
    Task<EditProfileViewModel> GetEditProfileViewModelAsync(string userId, string email);
    Task UpdateProfileAsync(string userId, EditProfileViewModel model);
    Task<MyFinancesViewModel> GetFinancesViewModelAsync(string userId);
    Task<(bool Success, string Message, decimal Amount)> WithdrawFundsAsync(string userId);
    
}