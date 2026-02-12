using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;

namespace Market.Web.Services;

public interface IAuctionProcessingService
{
    Task<List<AuctionImage>> ProcessUploadedImagesWebpAsync(List<IFormFile> photos);
    
    Task<List<MyAuctionViewModel>> GetUserAuctionsViewModelAsync(string userId);
}