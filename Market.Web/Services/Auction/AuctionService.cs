using Market.Web.Core.Models;
using Market.Web.Repositories;

namespace Market.Web.Services;

public class AuctionService : IAuctionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuctionProcessingService _processingService;

    public AuctionService(IUnitOfWork unitOfWork, IAuctionProcessingService processingService)
    {
        _unitOfWork = unitOfWork;
        _processingService = processingService;
    }
    
    public async Task<List<Auction>> GetAllAsync()
    {
        return await _unitOfWork.Auctions.GetAllAsync();
    }
    
    public async Task<Auction?> GetByIdAsync(int id)
    {
        return await _unitOfWork.Auctions.GetByIdAsync(id);
    }

    public async Task CreateAuctionAsync(Auction auction, List<IFormFile> photos)
    {
        auction.Images = await _processingService.ProcessUploadedImagesWebpAsync(photos);

        if (auction.CreatedAt == default) auction.CreatedAt = DateTime.Now;

        auction.AuctionStatus = AuctionStatus.Active;

        await _unitOfWork.Auctions.AddAsync(auction);
        await _unitOfWork.CompleteAsync();
    }

    public async Task UpdateAuctionAsync(Auction auction)
    {
        var auctionInDb = await _unitOfWork.Auctions.GetByIdAsync(auction.Id);
        if (auctionInDb == null) return;

        auctionInDb.Title = auction.Title;
        auctionInDb.Description = auction.Description;
        auctionInDb.Price = auction.Price;
        auctionInDb.Quantity = auction.Quantity;
        auctionInDb.Category = auction.Category;
        auctionInDb.EndDate = auction.EndDate;
        auctionInDb.IsCompanySale = auction.IsCompanySale;
        auctionInDb.GeneratedByAi = auction.GeneratedByAi;

        await _unitOfWork.CompleteAsync();
    }

    public async Task SoftDeleteAuctionAsync(int id)
    {
        var auction = await _unitOfWork.Auctions.GetByIdAsync(id);
        if (auction != null)
        {
            auction.AuctionStatus = AuctionStatus.Expired;
            await _unitOfWork.CompleteAsync();
        }
    }
    
}