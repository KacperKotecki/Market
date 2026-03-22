using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using Market.Web.Core.Models;
using Market.Web.Repositories;
using Market.Web.Core.Exceptions;

namespace Market.Web.Services;

public class AuctionImageWorker : IAuctionImageWorker
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionImageWorker> _logger;

    public AuctionImageWorker(
        IWebHostEnvironment webHostEnvironment,
        IServiceScopeFactory scopeFactory,
        ILogger<AuctionImageWorker> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ProcessImagesJobAsync(int auctionId, string[] tempPaths)
    {
        const long maxFileSize = 10 * 1024 * 1024;
        var images = new List<AuctionImage>();
        var createdWebpPaths = new List<string>();
        var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

        var encoder = new WebpEncoder { Quality = 75 };

        foreach (var tempPath in tempPaths)
        {
            if (!File.Exists(tempPath)) continue;

            try
            {
                var fileInfo = new FileInfo(tempPath);
                if (fileInfo.Length == 0 || fileInfo.Length > maxFileSize)
                {
                    continue;
                }

                var fileName = Guid.NewGuid().ToString() + ".webp";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var fileStream = File.OpenRead(tempPath))
                using (var image = await Image.LoadAsync(fileStream))
                {
                    if (image.Width > 1920 || image.Height > 1080)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(1920, 1080)
                        }));
                    }
                    await image.SaveAsync(filePath, encoder);
                }

                createdWebpPaths.Add(filePath);
                images.Add(new AuctionImage { AuctionId = auctionId, ImagePath = "/uploads/" + fileName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił błąd podczas przetwarzania obrazu (WebP): {TempPath}", tempPath);
            }
        }

        // Pula połączeń i transakcja bazodanowa
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        using var transaction = await unitOfWork.BeginTransactionAsync();
        try 
        {
            if (images.Count != 0)
            {
                await unitOfWork.Auctions.AddImagesAsync(images);
                await unitOfWork.CompleteAsync(); 
            }

            // Status Update powiązany z tą samą transakcją
            await unitOfWork.Auctions.UpdateStatusAsync(auctionId, AuctionStatus.ImagesReady);
            
            await transaction.CommitAsync();

            // Dopiero gdy transakcja z DB zakończy się sukcesem, możemy bezpiecznie usunąć stare tempPaths!
            foreach (var tempPath in tempPaths)
            {
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Nie udało się usunąć pliku tymczasowego po wymitowaniu transakcji: {TempPath}", tempPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Błąd podczas zapisu do DB dla aukcji {AuctionId}. Wycofywanie transakcji dyskowych i bazy.", auctionId);
            
            // Punkt bezpieczeństwa: usuwamy sieroty plikowe (.webp) i nie usuwamy .RAW, aby Hangfire mógł spróbować jeszcze raz
            foreach (var webpPath in createdWebpPaths.Where(File.Exists))
            {
                try { File.Delete(webpPath); } 
                catch { /* Ignorujemy błędy przy rollbacku dyskowym */ }
            }

            throw new AuctionProcessingException($"Krytyczny błąd zapisu dla aukcji: {auctionId}", ex);
        }
    }
}