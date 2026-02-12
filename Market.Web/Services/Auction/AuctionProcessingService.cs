using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using Market.Web.Repositories;

namespace Market.Web.Services;

public class AuctionProcessingService : IAuctionProcessingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AuctionProcessingService(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<List<AuctionImage>> ProcessUploadedImagesWebpAsync(List<IFormFile> photos)
    {
        const long maxFileSize = 10 * 1024 * 1024; 

        var images = new List<AuctionImage>();
        if (photos == null || photos.Count == 0) return images;

        var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

        var encoder = new WebpEncoder { Quality = 75 };

        foreach (var photo in photos)
        {
            if (photo.Length == 0 || photo.Length > maxFileSize) continue;

            try 
            {
                var fileName = Guid.NewGuid().ToString() + ".webp"; 
                var filePath = Path.Combine(uploadDir, fileName);

                using (var image = await Image.LoadAsync(photo.OpenReadStream()))
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

                images.Add(new AuctionImage { ImagePath = "/uploads/" + fileName });
            }
            catch (Exception)
            {
                continue;
            }
        }
        return images;
    }

    public async Task<List<MyAuctionViewModel>> GetUserAuctionsViewModelAsync(string userId)
    {
        var auctions = await _unitOfWork.Auctions.GetUserAuctionsAsync(userId);


        return auctions.Select(a => 
        {
            var opinionsList = a.Orders
                .Where(o => o.Opinion != null)
                .Select(o => new OpinionDetailViewModel 
                {
                    BuyerName = o.Opinion!.Buyer != null ? o.Opinion.Buyer.UserName : "Anonim",
                    Rating = o.Opinion.Rating,
                    Comment = o.Opinion.Comment,
                    DateFormatted = o.Opinion.CreatedAt.ToString("dd.MM.yyyy")
                })
                .ToList();

            return new MyAuctionViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                Price = a.Price,
                Category = a.Category,
                Quantity = a.Quantity,
                IsCompanySale = a.IsCompanySale,
                GeneratedByAi = a.GeneratedByAi,
                
                CreatedAtFormatted = a.CreatedAt.ToShortDateString(),
                EndDateFormatted = a.EndDate.ToShortDateString(),
                AuctionStatus = a.AuctionStatus, 

                IsBannedOrSuspended = a.AuctionStatus == AuctionStatus.Banned || a.AuctionStatus == AuctionStatus.Suspended,
                BannedNote = a.BannedNote,

                StatusBadgeClass = a.AuctionStatus switch
                {
                    AuctionStatus.Active => "bg-success",   // Zielony
                    AuctionStatus.Banned => "bg-danger",    // Czerwony
                    AuctionStatus.Suspended => "bg-danger", // Czerwony
                    AuctionStatus.Draft => "bg-secondary",  // Szary
                    AuctionStatus.Sold => "bg-dark",        // Czarny
                    AuctionStatus.Expired => "bg-secondary", // Wygasła
                    _ => "bg-primary"                       // Domyślny
                },

                // 2. Tekst statusu (PL)
                StatusLabel = a.AuctionStatus switch
                {
                    AuctionStatus.Active => "AKTYWNA",
                    AuctionStatus.Banned => "ZABLOKOWANA",
                    AuctionStatus.Suspended => "ZAWIESZONA",
                    AuctionStatus.Draft => "SZKIC",
                    AuctionStatus.Sold => "SPRZEDANA",
                    AuctionStatus.Expired => "WYGASŁA",
                    _ => a.AuctionStatus.ToString().ToUpper()
                },  

                ImageUrl = a.Images != null && a.Images.Any() 
                    ? a.Images.First().ImagePath 
                    : "https://via.placeholder.com/300x220?text=Brak+Zdjecia",

                Opinions = opinionsList
            };
        }).ToList();
    }
    
}