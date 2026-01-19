using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Market.Web.Data;
using Market.Web.Models;
using Market.Web.ViewModels;

namespace Market.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int auctionId)
        {
            var auction = await _context.Auctions
                .Include(a => a.User)   // Sprzedawca
                .Include(a => a.Images) // Zdjęcia (potrzebne do ImageUrl)
                .FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction == null || auction.Quantity <= 0 || auction.EndDate <= DateTime.Now)
            {
                return RedirectToAction("Index", "Auctions");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userProfile = await _context.UserProfiles
                .Include(p => p.CompanyProfile)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (auction.UserId == user.Id)
            {
                 TempData["Error"] = "Nie możesz kupić własnego przedmiotu.";
                 return RedirectToAction("Details", "Auctions", new { id = auction.Id });
            }

            var model = new CheckoutViewModel
            {
                AuctionId = auction.Id,
                AuctionTitle = auction.Title,
                Price = auction.Price,
                IsCompanySale = auction.IsCompanySale,
                SellerName = auction.User?.UserName ?? "Nieznany",
                ImageUrl = auction.Images.FirstOrDefault()?.ImagePath ?? "/img/placeholder.png", // Pobieramy pierwsze zdjęcie

                BuyerName = userProfile != null ? $"{userProfile.FirstName} {userProfile.LastName}" : user.Email!,
                ShippingAddress = userProfile?.ShippingAddress ?? new Address(),

                BuyerHasCompanyProfile = userProfile?.CompanyProfile != null,
                BuyerCompanyName = userProfile?.CompanyProfile?.CompanyName,
                BuyerNIP = userProfile?.CompanyProfile?.NIP,
                BuyerInvoiceAddress = userProfile?.CompanyProfile?.InvoiceAddress
            };

            return View(model);
        }

        // POST: /Order/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == model.AuctionId);
            
            if (auction == null || auction.Quantity <= 0) 
            {
                TempData["Error"] = "Przedmiot został właśnie sprzedany lub aukcja wygasła.";
                return RedirectToAction("Index", "Auctions");
            }

            var user = await _userManager.GetUserAsync(User);
            var userProfile = await _context.UserProfiles
                .Include(p => p.CompanyProfile)
                .FirstOrDefaultAsync(p => p.UserId == user!.Id);

            if (model.WantsInvoice && userProfile?.CompanyProfile == null)
            {
                ModelState.AddModelError("", "Aby otrzymać fakturę, musisz uzupełnić dane firmy w profilu.");
                TempData["Error"] = "Błąd: Brak danych firmowych.";
                return RedirectToAction("EditProfile", "Profile"); 
            }

            var order = new Order
            {
                AuctionId = auction.Id,
                BuyerId = user!.Id,
                TotalPrice = auction.Price,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                IsCompanyPurchase = model.WantsInvoice
            };

            if (model.WantsInvoice && userProfile?.CompanyProfile != null)
            {
                var cp = userProfile.CompanyProfile;
                order.BuyerCompanyName = cp.CompanyName;
                order.BuyerNIP = cp.NIP;
                order.BuyerInvoiceAddress = $"{cp.InvoiceAddress.Street}, {cp.InvoiceAddress.PostalCode} {cp.InvoiceAddress.City}, {cp.InvoiceAddress.Country}";
            }

            _context.Orders.Add(order);

            auction.Quantity -= 1;
            if (auction.Quantity == 0)
            {
                auction.AuctionStatus = AuctionStatus.Sold;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("OrderConfirmation", new { id = order.Id });
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var orders = await _context.Orders
                .Include(o => o.Auction)
                    .ThenInclude(a => a.Images)
                .Include(o => o.Auction)
                    .ThenInclude(a => a.User) // Aby pobrać nazwę sprzedawcy
                .Where(o => o.BuyerId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new MyOrderViewModel
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status,
                    IsCompanyPurchase = o.IsCompanyPurchase,
                    
                    AuctionId = o.AuctionId,
                    AuctionTitle = o.Auction != null ? o.Auction.Title : "Oferta usunięta",
                    SellerName = o.Auction != null && o.Auction.User != null ? o.Auction.User.UserName : "Nieznany",
                    
                    ImageUrl = o.Auction != null && o.Auction.Images.Any() 
                        ? o.Auction.Images.First().ImagePath 
                        : "https://via.placeholder.com/150"
                })
                .ToListAsync();

            return View(orders);
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }
    }
}