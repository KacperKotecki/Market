using Market.Web.Core.Models;

namespace Market.Web.Services;

public interface IAuctionService
{
    Task<List<Auction>> GetAllAsync();
    Task<Auction?> GetByIdAsync(int id);
    
    Task CreateAuctionAsync(Auction auction, List<IFormFile> photos);
    Task UpdateAuctionAsync(Auction auction);
    Task SoftDeleteAuctionAsync(int id);
}