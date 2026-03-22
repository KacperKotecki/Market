using Market.Web.Core.Models;
using Market.Web.Repositories;
using Market.Web.Core.Exceptions;
using Hangfire;

namespace Market.Web.Services.AI;

public class AiWorker : IAiWorker
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiWorker> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AiWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AiWorker> logger,
        IWebHostEnvironment webHostEnvironment)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task GenerateDescriptionJobAsync(int auctionId)
    {
        var webpPaths = new List<string>();

        using (var preScope = _scopeFactory.CreateScope())
        {
            var unitOfWork = preScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var auction = await unitOfWork.Auctions.GetByIdAsync(auctionId);

            if (auction == null)
            {
                _logger.LogWarning("Nie odnaleziono aukcji o ID: {auctionId} podczas odpalania joba AI.", auctionId);
                return;
            }

            foreach (var img in auction.Images)
            {
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, img.ImagePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    webpPaths.Add(fullPath);
                }
            }
        }

        Market.Web.Core.DTOs.AuctionDraftDto generatedData;

        try
        {
            using (var aiScope = _scopeFactory.CreateScope())
            {
                var aiService = aiScope.ServiceProvider.GetRequiredService<IADescriptionService>();
                generatedData = await aiService.GenerateFromWebPFilesAsync(webpPaths);
            }
        }
        catch (AiGenerationException ex)
        {
            _logger.LogError(ex, "Cataestrophic failure during AI generation for auction {AuctionId}", auctionId);
            
            // Catastrophic failure requires reverting state, as per instructions.
            using (var failScope = _scopeFactory.CreateScope())
            {
                var failUnitOfWork = failScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await failUnitOfWork.Auctions.UpdateStatusAsync(auctionId, AuctionStatus.AiGenerationFailed);
            }
            throw; // Re-throw to Hangfire.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during AI generation for auction {AuctionId}", auctionId);

            using (var failScope = _scopeFactory.CreateScope())
            {
                var failUnitOfWork = failScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await failUnitOfWork.Auctions.UpdateStatusAsync(auctionId, AuctionStatus.AiGenerationFailed);
            }
            throw new AiGenerationException($"Niespodziewany błąd Joba AI dla rzutu: {auctionId}", ex);
        }

        using (var saveScope = _scopeFactory.CreateScope())
        {
            var postUnitOfWork = saveScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await postUnitOfWork.Auctions.UpdateAiDataAsync(
                auctionId, 
                generatedData.Title ?? string.Empty, 
                generatedData.Description ?? string.Empty, 
                generatedData.Category ?? string.Empty, 
                generatedData.SuggestedPrice);
        }
    }
}