using Market.Web.Core.DTOs;
using Market.Web.Core.Models;

namespace Market.Web.Repositories;

public interface IAuctionRepository
{
    Task<List<Auction>> GetAllAsync();
    Task<(List<Auction> Items, int TotalCount)> GetAllWithFiltersAsync(AuctionFilter filter);
    
    Task<List<Auction>> GetUserAuctionsAsync(string userId); 
    Task<Auction?> GetByIdAsync(int id);
    
    Task AddAsync(Auction auction);
    
    Task UpdateAsync(Auction auction);

    Task UpdateStatusAsync(int auctionId, AuctionStatus status);

    Task UpdateAiDataAsync(int auctionId, string title, string description, string category, decimal? suggestedPrice);

    Task AddImagesAsync(IEnumerable<AuctionImage> images);
}