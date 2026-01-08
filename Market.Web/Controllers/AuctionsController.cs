using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Market.Web.Data;
using Market.Web.Models;
using Market.Web.Repositories; // -- 1. Dodano brakujący using

namespace Market.Web.Controllers;

[Authorize] 
public class AuctionsController : Controller
{
    private readonly IAuctionRepository _repository;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuctionsController(IAuctionRepository repository, UserManager<ApplicationUser> userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    // GET: Auctions (Zaktualizowano o parametry filtrowania)
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

    [AllowAnonymous]
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
    // GET: Auctions/Create (Formularz)
    public IActionResult Create()
    {
        return View();
    }

    // POST: Auctions/Create (Odbiór danych z formularza)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Auction auction)
    {
        
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge(); 

        
        auction.UserId = user.Id;
        auction.CreatedAt = DateTime.Now;
        auction.AuctionStatus = AuctionStatus.Active;
        
        
        ModelState.Remove("UserId");
        ModelState.Remove("User");

        
        if (ModelState.IsValid)
        {
            await _repository.AddAsync(auction);
            await _repository.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        return View(auction);
    }
    
    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user;
    }
}