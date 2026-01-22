using Market.Web.Data;
using Market.Web.Models;
using Market.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Market.Web.Services;

public class AuctionProcessingService : IAuctionProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AuctionProcessingService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<List<AuctionImage>> ProcessUploadedImagesAsync(List<IFormFile> photos)
    {
        var images = new List<AuctionImage>();
        if (photos == null || photos.Count == 0) return images;

        var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

        foreach (var photo in photos)
        {
            if (photo.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }
                
                images.Add(new AuctionImage { ImagePath = "/uploads/" + fileName });
            }
        }
        return images;
    }

    public async Task<List<MyAuctionViewModel>> GetUserAuctionsViewModelAsync(string userId)
    {
        var auctions = await _context.Auctions
            .Include(a => a.Images)
            .Include(a => a.Orders)
                .ThenInclude(o => o.Opinion)
                    .ThenInclude(op => op.Buyer)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

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