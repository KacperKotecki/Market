using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Market.Web.Services;

namespace Market.Web.Controllers;

[Authorize(Roles = "Admin")] // Kluczowe zabezpieczenie! Tylko admin wejdzie.
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
}