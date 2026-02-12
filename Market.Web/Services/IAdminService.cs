using Market.Web.Core.DTOs;
using Market.Web.Core.ViewModels;

namespace Market.Web.Services;

public interface IAdminService
{
    Task<UsersListViewModel> GetUsersAsync(string searchString, string sortOrder, int pageNumber, int pageSize);
    Task ToggleUserBlockStatusAsync(string userId);

    Task<AdminAuctionsListViewModel> GetAuctionsAsync(string searchString, string sortOrder, int pageNumber, int pageSize);
    Task ToggleAuctionBanAsync(int auctionId, string? reason);
}