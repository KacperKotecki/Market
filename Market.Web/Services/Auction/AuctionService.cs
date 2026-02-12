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

        await _unitOfWork.Auctions.AddAsync(auction);
        await _unitOfWork.CompleteAsync();
    }

    public async Task UpdateAuctionAsync(Auction auction)
    {
        await _unitOfWork.Auctions.UpdateAsync(auction);
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