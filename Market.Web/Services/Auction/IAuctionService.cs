using Market.Web.Core.DTOs;
using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Market.Web.Services;

public interface IAuctionService
{
    Task<List<Auction>> GetAllAsync();
    Task<AuctionListViewModel> GetAllWithFiltersAsync(AuctionFilter filter);
    Task<Auction?> GetByIdAsync(int id);
    
    Task CreateAuctionAsync(AuctionFormViewModel vm, string userId, List<IFormFile> photos);
    Task UpdateAuctionAsync(AuctionFormViewModel vm, string requestingUserId);
    Task SoftDeleteAuctionAsync(int id);
}