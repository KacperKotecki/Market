using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Market.Web.Core.Models;

namespace Market.Web.Controllers;

public class HomeController : Controller
{

    public HomeController()
    {
    }

    [HttpGet("demo-access-preview-t5khy79jyuxby2lolnx4n5r90z58")] 
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult RedirectToApp()
    {
        return RedirectToAction("Index", "Auctions");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
