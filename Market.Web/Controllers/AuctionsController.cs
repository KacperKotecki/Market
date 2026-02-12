using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Market.Web.Core.Models;
using Market.Web.Services;
using Market.Web.Authorization;

namespace Market.Web.Controllers;

[Authorize] 
public class AuctionsController : Controller
{
    private readonly IAuctionService _auctionService; 
    private readonly IProfileService _profileService;
    private readonly IADescriptionService _aiDescriptionService;
    private readonly IAuctionProcessingService _auctionProcessingService;
    private readonly UserManager<ApplicationUser> _userManager;


    public AuctionsController(
        IAuctionService auctionService,
        IProfileService profileService,
        IADescriptionService aiDescriptionService,
        IAuctionProcessingService auctionProcessingService,
        UserManager<ApplicationUser> userManager)
    {
        _auctionService = auctionService;
        _profileService = profileService;
        _aiDescriptionService = aiDescriptionService;
        _auctionProcessingService = auctionProcessingService;
        _userManager = userManager;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? searchString, string? category, decimal? minPrice, decimal? maxPrice, string? sortOrder)
    {
        var auctions = await _auctionService.GetAllAsync();

        IEnumerable<Auction> query = auctions
            .Where(a => a.AuctionStatus == AuctionStatus.Active && a.EndDate > DateTime.Now);

        if (!string.IsNullOrEmpty(searchString))
            query = query.Where(s => s.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase) || s.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        
        if (!string.IsNullOrEmpty(category)) query = query.Where(x => x.Category == category);
        if (minPrice.HasValue) query = query.Where(x => x.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(x => x.Price <= maxPrice.Value);

        query = sortOrder switch {
            "price_desc" => query.OrderByDescending(s => s.Price),
            "price_asc" => query.OrderBy(s => s.Price),
            _ => query.OrderByDescending(s => s.CreatedAt),
        };
        return View(query.ToList());
    }

    [Buyer]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var auction = await _auctionService.GetByIdAsync(id.Value);
        return auction == null ? NotFound() : View(auction);
    }

    [Seller]
    public async Task<IActionResult> Create()
    {
        var user = await GetCurrentUserAsync();

        var userProfile = user != null ? await _profileService.GetByUserIdAsync(user.Id) : null;
        bool hasCompanyProfile = userProfile?.CompanyProfile != null;

        ViewBag.CanSellAsCompany = hasCompanyProfile;

        return View(new Auction { EndDate = DateTime.Now.AddDays(30) });
    }

    [Seller]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Auction auction, List<IFormFile> photos)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        auction.UserId = user.Id;

        if (auction.EndDate <= DateTime.Now)
            ModelState.AddModelError("EndDate", "Data zakończenia musi być w przyszłości.");

        if (ModelState.IsValid)
        {
            await _auctionService.CreateAuctionAsync(auction, photos);
            
            return RedirectToAction(nameof(Index));
        }
        return View(auction);
    }
 
    [Seller]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var auction = await _auctionService.GetByIdAsync(id.Value);
        if (auction == null) return NotFound();

        var user = await GetCurrentUserAsync();
        if (user == null || auction.UserId != user.Id) return Forbid();

        return View(auction);
    }
    

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Auction auction) 
    {
        if (ModelState.IsValid)
        {
            await _auctionService.UpdateAuctionAsync(auction);
            return RedirectToAction(nameof(MyAuctions));
        }
        return View(auction);
    }
    [Seller]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var auctionToExpire = await _auctionService.GetByIdAsync(id);
        
        if (auctionToExpire == null) return NotFound();

        if (auctionToExpire.UserId != user.Id) return Forbid();

        await _auctionService.SoftDeleteAuctionAsync(id);

        TempData["SuccessMessage"] = "Oferta została zakończona.";
        return RedirectToAction(nameof(MyAuctions));
    }
    [Seller]
    [Authorize]
    public async Task<IActionResult> MyAuctions()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var model = await _auctionProcessingService.GetUserAuctionsViewModelAsync(user.Id);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateDescription(List<IFormFile> photos)
    {
        if (photos == null || photos.Count == 0) return BadRequest(new { error = "Brak zdjęć." });
        try
        {
            return Json(await _aiDescriptionService.GenerateFromImagesAsync(photos));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Błąd AI: " + ex.Message });
        }
    }

    private async Task<ApplicationUser> GetCurrentUserAsync() => await _userManager.GetUserAsync(User);
}