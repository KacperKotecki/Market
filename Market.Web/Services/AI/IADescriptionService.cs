using Market.Web.Core.DTOs;

namespace Market.Web.Services.AI;

public interface IADescriptionService
{
    Task<AuctionDraftDto> GenerateFromImagesAsync(List<IFormFile> images);
    Task<AuctionDraftDto> GenerateFromWebPFilesAsync(List<string> imagePaths);
}