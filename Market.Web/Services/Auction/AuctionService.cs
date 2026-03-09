using Market.Web.Core.DTOs;
using Market.Web.Core.Exceptions;
using Market.Web.Core.Helpers;
using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;
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

    public async Task<AuctionListViewModel> GetAllWithFiltersAsync(AuctionFilter filter)
    {
        var (items, totalCount) = await _unitOfWork.Auctions.GetAllWithFiltersAsync(filter);

        int totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

        var dtos = items.Select(a => new AuctionSummaryDto
        {
            Id             = a.Id,
            Title          = a.Title,
            Description    = a.Description,
            Price          = a.Price,
            Category       = a.Category,
            AuctionStatus  = a.AuctionStatus,
            EndDate        = a.EndDate,
            CreatedAt      = a.CreatedAt,
            Quantity       = a.Quantity,
            GeneratedByAi  = a.GeneratedByAi,
            ThumbnailPath  = a.Images?.FirstOrDefault()?.ImagePath,
            SellerUserName = a.User?.UserName ?? string.Empty,
            SellerId       = a.UserId ?? string.Empty,
            IsCompanySale  = a.IsCompanySale,
        }).ToList();

        return new AuctionListViewModel
        {
            Auctions     = dtos,
            SearchString = filter.SearchString,
            Category     = filter.Category,
            MinPrice     = filter.MinPrice,
            MaxPrice     = filter.MaxPrice,
            SortOrder    = filter.SortOrder,
            CurrentPage  = filter.PageNumber,
            TotalPages   = totalPages,
        };
    }
    
    public async Task<Auction?> GetByIdAsync(int id)
    {
        return await _unitOfWork.Auctions.GetByIdAsync(id);
    }

    public async Task CreateAuctionAsync(AuctionFormViewModel vm, string userId, List<IFormFile> photos)
    {
        var endDateUtc = vm.EndDate.ToUtcFromPolandTime(); // throws AuctionException on DST gap

        if (endDateUtc <= DateTime.UtcNow)
            throw new AuctionException(
                "Data zakończenia musi być w przyszłości.",
                nameof(vm.EndDate));

        var auction = new Auction
        {
            Title         = vm.Title,
            Description   = vm.Description,
            Price         = vm.Price,
            Quantity      = vm.Quantity,
            Category      = vm.Category,
            EndDate       = endDateUtc,
            IsCompanySale = vm.IsCompanySale,
            GeneratedByAi = vm.GeneratedByAi,
            UserId        = userId,
            CreatedAt     = DateTime.UtcNow,
            AuctionStatus = AuctionStatus.Active,
        };

        auction.Images = await _processingService.ProcessUploadedImagesWebpAsync(photos);

        await _unitOfWork.Auctions.AddAsync(auction);
        await _unitOfWork.CompleteAsync();
    }

    public async Task UpdateAuctionAsync(AuctionFormViewModel vm, string requestingUserId)
    {
        var auctionInDb = await _unitOfWork.Auctions.GetByIdAsync(vm.Id);
        if (auctionInDb == null) return;

        if (auctionInDb.UserId != requestingUserId)
            throw new OrderAuthorizationException("Brak uprawnień do edycji tej aukcji.");

        var endDateUtc = vm.EndDate.ToUtcFromPolandTime(); // throws AuctionException on DST gap

        if (endDateUtc <= DateTime.UtcNow)
            throw new AuctionException(
                "Data zakończenia musi być w przyszłości.",
                nameof(vm.EndDate));

        auctionInDb.Title         = vm.Title;
        auctionInDb.Description   = vm.Description;
        auctionInDb.Price         = vm.Price;
        auctionInDb.Quantity      = vm.Quantity;
        auctionInDb.Category      = vm.Category;
        auctionInDb.EndDate       = endDateUtc;
        auctionInDb.IsCompanySale = vm.IsCompanySale;
        auctionInDb.GeneratedByAi = vm.GeneratedByAi;

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