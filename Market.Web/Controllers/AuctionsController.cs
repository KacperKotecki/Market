using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Market.Web.Data;
using Market.Web.Models;
using Market.Web.Repositories;
using Market.Web.Services;
using Market.Web.Authorization;

namespace Market.Web.Controllers;

[Authorize] 
public class AuctionsController : Controller
{
    private readonly IAuctionRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IADescriptionService _aiService;
    private readonly Market.Web.Data.ApplicationDbContext _context; 

    public AuctionsController(IAuctionRepository repository, UserManager<ApplicationUser> userManager, IADescriptionService aiService, Market.Web.Data.ApplicationDbContext context)
    {
        _repository = repository;
        _userManager = userManager;
        _aiService = aiService;
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? searchString, string? category, decimal? minPrice, decimal? maxPrice, string? sortOrder)
    {

        var auctions = await _repository.GetAllAsync();

        IEnumerable<Auction> query = auctions;

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(s => s.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase) 
                                  || s.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(x => x.Category == category);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x => x.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= maxPrice.Value);
        }

        query = sortOrder switch
        {
            "price_desc" => query.OrderByDescending(s => s.Price),
            "price_asc" => query.OrderBy(s => s.Price),
            _ => query.OrderByDescending(s => s.CreatedAt), // Domyślne: od najnowszych
        };

        return View(query.ToList());
    }

    [Buyer]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var auction = await _repository.GetByIdAsync(id.Value);

        if (auction == null)
        {
            return NotFound();
        }

        return View(auction);
    }
    [Seller]
    public async Task<IActionResult> Create()
    {
        var user = await GetCurrentUserAsync();
        
        bool hasCompanyProfile = false;
        if (user != null)
        {
             hasCompanyProfile = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                _context.CompanyProfiles, cp => cp.UserProfile.UserId == user.Id);
        }

        ViewBag.CanSellAsCompany = hasCompanyProfile;

        var model = new Auction
        {
            EndDate = DateTime.Now.AddDays(30) 
        };
        return View(model);
    }

    [Seller]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Auction auction, List<IFormFile> photos)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge(); 

        auction.UserId = user.Id;
        auction.CreatedAt = DateTime.Now;
        auction.AuctionStatus = AuctionStatus.Active;
        auction.Quantity = auction.Quantity;

        if (auction.EndDate <= DateTime.Now)
            ModelState.AddModelError("EndDate", "Data zakończenia aukcji musi być późniejsza niż dzisiejsza.");
        else
            auction.EndDate = auction.EndDate;
        
        ModelState.Remove("UserId");
        ModelState.Remove("User");
        ModelState.Remove("Images"); 

        if (ModelState.IsValid)
        {
            if (photos != null && photos.Count > 0)
            {
                foreach (var photo in photos)
                {
                    if (photo.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                        
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        
                        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                        var filePath = Path.Combine(uploadDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(stream);
                        }
                        auction.Images.Add(new AuctionImage 
                        { 
                            ImagePath = "/uploads/" + fileName 
                        });
                    }
                }
            }

            await _repository.AddAsync(auction);
            await _repository.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        return View(auction);
    }
    [Seller]
    public async Task<IActionResult> Edit(int? id) // Poprawiłem literówkę iActionResult -> IActionResult
    {
        if (id == null)
        {
            return NotFound();
        }

        var auction = await _repository.GetByIdAsync(id.Value);
        if (auction == null)
        {
            return NotFound();
        }

        // SPRAWDZENIE BEZPIECZEŃSTWA: Czy edytujący to właściciel?
        var user = await GetCurrentUserAsync();
        if (user == null || auction.UserId != user.Id)
        {
            return Forbid(); // Zwraca brak dostępu
        }

        return View(auction);
    }
    [Seller]
    [HttpPost]
    [ValidateAntiForgeryToken]

    public async Task<IActionResult> Edit(int id, Auction auction, List<IFormFile> photos)
    {
        if (id != auction.Id)
        {
            return NotFound();
        }
        var auctionToUpdate = await _repository.GetByIdAsync(id);
        
        if (auctionToUpdate == null) return NotFound();

        var user = await GetCurrentUserAsync();
        if (user == null || auctionToUpdate.UserId != user.Id)
        {
            return Forbid();
        }

        ModelState.Remove("UserId");
        ModelState.Remove("User");
        ModelState.Remove("Images");

        if (ModelState.IsValid)
        {
            auctionToUpdate.Title = auction.Title;
            auctionToUpdate.Description = auction.Description;
            auctionToUpdate.Price = auction.Price;
            auctionToUpdate.Category = auction.Category;
            auctionToUpdate.Quantity = auction.Quantity;

            if (auction.EndDate <= DateTime.Now)
            {
                ModelState.AddModelError("EndDate", "Data zakończenia aukcji musi być późniejsza niż dzisiejsza.");
                return View(auctionToUpdate); 
            }
            else
                auctionToUpdate.EndDate = auction.EndDate;

            if (photos != null && photos.Count > 0)
            {
                foreach (var photo in photos)
                {
                    if (photo.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
                        var filePath = Path.Combine(uploadDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(stream);
                        }

                        if (auctionToUpdate.Images == null) 
                            auctionToUpdate.Images = new List<AuctionImage>();
                            
                        auctionToUpdate.Images.Add(new AuctionImage 
                        { 
                            ImagePath = "/uploads/" + fileName 
                        });
                    }
                }
            }

            await _repository.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }
        
        return View(auctionToUpdate);
    }
        [HttpPost]
    public async Task<IActionResult> GenerateDescription(List<IFormFile> photos)
    {
        if (photos == null || photos.Count == 0)
        {
            return BadRequest(new { error = "Nie przesłano żadnych zdjęć." });
        }

        try
        {
            var draft = await _aiService.GenerateFromImagesAsync(photos);
            
            return Json(draft);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Błąd generowania AI: " + ex.Message });
        }
    }
    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user;
    }
}