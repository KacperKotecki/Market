using Market.Web.Core.DTOs;
using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;

namespace Market.Web.Services;

public interface IAuctionProcessingService
{
    Task<List<AuctionImage>> ProcessUploadedImagesWebpAsync(List<IFormFile> photos);
    
    Task<List<MyAuctionViewModel>> GetUserAuctionsViewModelAsync(string userId);

    Task<AuctionDetailsViewModel?> GetAuctionDetailsViewModelAsync(int id);
    Task<AuctionFormViewModel?> GetAuctionFormViewModelAsync(int id);
    Task ScheduleAiGenerationAsync(int auctionId);
    Task<int> CreateImageProcessingDraftAsync(string userId, List<string> tempPaths);
    Task<AuctionStatusDto?> GetAuctionStatusAsync(int id);
    Task CleanupTemporaryFilesJobAsync();
}