using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Market.Web.Services;

namespace Market.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<IActionResult> Index(string searchString, string sortOrder, int pageNumber = 1)
    {
        int pageSize = 10; 
        
        var model = await _adminService.GetUsersAsync(searchString, sortOrder, pageNumber, pageSize);
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlockUser(string id)
    {
        await _adminService.ToggleUserBlockStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Auctions(string searchString, string sortOrder, int pageNumber = 1)
    {
        int pageSize = 10;
        var model = await _adminService.GetAuctionsAsync(searchString, sortOrder, pageNumber, pageSize);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAuctionBan(int id, string? reason)
    {
        await _adminService.ToggleAuctionBanAsync(id, reason);
        return RedirectToAction(nameof(Auctions));
    }
}