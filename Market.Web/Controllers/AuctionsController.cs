using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Market.Web.Core.DTOs;
using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;
using Market.Web.Services;
using Market.Web.Services.AI;
using Market.Web.Authorization;
using Market.Web.Core.Exceptions;
using Market.Web.Core.Helpers;

namespace Market.Web.Controllers;

[Authorize] 
public class AuctionsController : Controller
{
    private readonly IAuctionService _auctionService; 
    private readonly IProfileService _profileService;
    private readonly IADescriptionService _aiDescriptionService;
    private readonly IAuctionProcessingService _auctionProcessingService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuctionsController> _logger;


    public AuctionsController(
        IAuctionService auctionService,
        IProfileService profileService,
        IADescriptionService aiDescriptionService,
        IAuctionProcessingService auctionProcessingService,
        UserManager<ApplicationUser> userManager,
        ILogger<AuctionsController> logger)
    {
        _auctionService = auctionService;
        _profileService = profileService;
        _aiDescriptionService = aiDescriptionService;
        _auctionProcessingService = auctionProcessingService;
        _userManager = userManager;
        _logger = logger;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(AuctionFilter filter)
    {
        filter.PageSize = 20;
        filter.Status = AuctionStatus.Active;
        var model = await _auctionService.GetAllWithFiltersAsync(filter);
        
        return View(model);
        
    }
    [AllowAnonymous]
    public async Task<IActionResult> Search(AuctionFilter filter)
    {
        filter.PageSize = 20;
        filter.Status = AuctionStatus.Active;
        var model = await _auctionService.GetAllWithFiltersAsync(filter);
        
        return PartialView("_AuctionListPartial",model);
    }

    [Buyer]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var auctionViewModel = await _auctionProcessingService.GetAuctionDetailsViewModelAsync(id.Value);
        return auctionViewModel == null ? NotFound() : View(auctionViewModel);
    }

    [Seller]
    public async Task<IActionResult> Create()
    {
        var user = await GetCurrentUserAsync();

        var userProfile = user != null ? await _profileService.GetByUserIdAsync(user.Id) : null;
        bool hasCompanyProfile = userProfile?.CompanyProfile != null;

        ViewBag.CanSellAsCompany = hasCompanyProfile;

        return View(new AuctionFormViewModel { EndDate = DateTime.UtcNow.AddDays(30).ToPolandTime() });
    }

    [Seller]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AuctionFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (vm.Id <= 0)
        {
            ModelState.AddModelError("", "Wystąpił błąd podczas dodawania zdjęć (brak ID aukcji). Upewnij się, że zdjęcia zostały załadowane prawidłowo.");
            return View(vm);
        }

        try
        {
            await _auctionService.FinalizeCreateAuctionAsync(vm, user.Id);
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }
        catch (AuctionException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? "EndDate", ex.Message);
        }
        catch (OrderAuthorizationException)
        {
            return Forbid();
        }

        return View(vm);
    }
 
    [Seller]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var vm = await _auctionProcessingService.GetAuctionFormViewModelAsync(id.Value);
        if (vm == null) return NotFound();

        var user = await GetCurrentUserAsync();
        if (user == null || vm.SellerId != user.Id) return Forbid();

        return View(vm);
    }
    

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Seller]
    public async Task<IActionResult> Edit(int id, AuctionFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        try
        {
            await _auctionService.UpdateAuctionAsync(vm, user.Id);
            return RedirectToAction(nameof(MyAuctions));
        }
        catch (AuctionException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? "EndDate", ex.Message);
        }
        catch (OrderAuthorizationException)
        {
            return Forbid();
        }

        return View(vm);
    }
    [Seller]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoftDelete(int id)
    {
        if (!ModelState.IsValid) return BadRequest();

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
    public async Task<IActionResult> MyAuctions()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var model = await _auctionProcessingService.GetUserAuctionsViewModelAsync(user.Id);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Seller]
    public async Task<IActionResult> GenerateDescription(List<IFormFile> photos)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (photos == null || photos.Count == 0) return BadRequest(new { error = "Brak zdjęć." });
        try
        {
            return Json(await _aiDescriptionService.GenerateFromImagesAsync(photos));
        }
        catch (AiGenerationException ex)
        {
            _logger.LogWarning(ex, "AI description generation failed");
            return StatusCode(500, new { error = "Błąd AI: " + ex.Message });
        }
    }

    private async Task<ApplicationUser> GetCurrentUserAsync() => await _userManager.GetUserAsync(User);
}