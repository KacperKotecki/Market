using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Market.Web.Data;
using Market.Web.Models;
using Market.Web.ViewModels;
using Market.Web.Services.Payments;

namespace Market.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StripePaymentService _stripePaymentService;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, StripePaymentService stripePaymentService)
        {
            _context = context;
            _userManager = userManager;
            _stripePaymentService = stripePaymentService;
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


            auction.Quantity -= 1;
            if (auction.Quantity == 0)
            {
                auction.AuctionStatus = AuctionStatus.Sold;
            }

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();
            
            try
            {
                var domain = $"{Request.Scheme}://{Request.Host}";
                
                var checkoutUrl = await _stripePaymentService.CreateCheckoutSession(order, domain);
                
                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                auction.Quantity += 1;
                if (auction.AuctionStatus == AuctionStatus.Sold) 
                    auction.AuctionStatus = AuctionStatus.Active;
                
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                TempData["Error"] = "Błąd inicjalizacji płatności. Spróbuj ponownie później.";
                return RedirectToAction("Details", "Auctions", new { id = model.AuctionId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int orderId, string session_id)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();
            
            return View("OrderConfirmation", order.Id);
        }

        [HttpGet]
        public IActionResult PaymentCancel(int orderId)
        {
            TempData["Error"] = "Płatność została anulowana. Możesz spróbować ponownie w 'Moje Zakupy'.";
            return RedirectToAction(nameof(MyOrders));
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
                    .ThenInclude(a => a.User)
                .Include(o => o.Opinion) 
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

                    HasOpinion = o.Opinion != null,
                    MyRating = o.Opinion != null ? o.Opinion.Rating : 0,

                    ImageUrl = o.Auction != null && o.Auction.Images.Any() 
                        ? o.Auction.Images.First().ImagePath 
                        : "https://via.placeholder.com/150"
                })
                .ToListAsync();

            return View(orders);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MySales()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var sales = await _context.Orders
                .Include(o => o.Auction)
                .Include(o => o.Buyer) 
                    .ThenInclude(b => b.UserProfile)
                .Where(o => o.Auction.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new MySaleViewModel
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status,
                    AuctionId = o.AuctionId,
                    AuctionTitle = o.Auction.Title,
                    
                    BuyerName = o.Buyer.UserName, 
                    BuyerEmail = o.Buyer.Email,

                    IsCompanyPurchase = o.IsCompanyPurchase,
                    ShippingAddressString = o.Buyer.UserProfile != null && o.Buyer.UserProfile.ShippingAddress != null
                        ? $"{o.Buyer.UserProfile.ShippingAddress.Street}, {o.Buyer.UserProfile.ShippingAddress.PostalCode} {o.Buyer.UserProfile.ShippingAddress.City}"
                        : "Brak danych adresowych",
                    
                    InvoiceDataString = o.IsCompanyPurchase 
                        ? $"{o.BuyerCompanyName} (NIP: {o.BuyerNIP})\n{o.BuyerInvoiceAddress}" 
                        : null
                })
                .ToListAsync();

            return View(sales);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.Auction)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            if (order.Auction.UserId != user.Id)
            {
                return Forbid();
            }

            order.Status = newStatus;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Zmieniono status zamówienia #{order.Id} na {newStatus}.";
            return RedirectToAction(nameof(MySales));
        }

                [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelivery(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.Auction)
                    .ThenInclude(a => a.User) 
                .Include(o => o.Auction)
                    .ThenInclude(a => a.User.UserProfile) 
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            if (order.BuyerId != user.Id)
            {
                return Forbid();
            }

            if (order.Status != OrderStatus.Shipped)
            {
                 TempData["Error"] = "Nie możesz potwierdzić odbioru dla tego statusu zamówienia.";
                 return RedirectToAction(nameof(MyOrders));
            }

            order.Status = OrderStatus.Completed;

            if(order.Auction != null && order.Auction.User != null && order.Auction.User.UserProfile != null)
            {
                order.Auction.User.UserProfile.WalletBalance += order.TotalPrice;
            }
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Potwierdzono odbiór. Transakcja zakończona, środki przekazane sprzedawcy.";
            return RedirectToAction(nameof(MyOrders));
        }


        [HttpGet]
        public async Task<IActionResult> Rate(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.Auction)
                    .ThenInclude(a => a.User)
                .Include(o => o.Auction)
                    .ThenInclude(a => a.Images)
                .Include(o => o.Opinion) // Ważne: sprawdzamy czy już jest opinia
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (order.BuyerId != user.Id) return Forbid();

            if (order.Status == OrderStatus.Pending) 
                return BadRequest("Nie możesz ocenić nieopłaconego zamówienia.");

            if (order.Opinion != null)
                return BadRequest("To zamówienie zostało już ocenione.");

            var model = new RateOrderViewModel
            {
                OrderId = order.Id,
                AuctionTitle = order.Auction?.Title ?? "Przedmiot usunięty",
                SellerName = order.Auction?.User?.UserName ?? "Nieznany",
                ImageUrl = order.Auction?.Images?.FirstOrDefault()?.ImagePath ?? "https://via.placeholder.com/150"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(RateOrderViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.Auction) // Potrzebne ID sprzedawcy
                .Include(o => o.Opinion)
                .FirstOrDefaultAsync(o => o.Id == model.OrderId);

            if (order == null || order.BuyerId != user.Id) return Forbid();
            if (order.Opinion != null) return BadRequest("Już oceniono.");
            if (order.Auction == null) return BadRequest("Nie można ocenić (aukcja nie istnieje).");

            var opinion = new Opinion
            {
                OrderId = order.Id,
                BuyerId = user.Id,
                SellerId = order.Auction.UserId,
                Comment = model.Comment,
                Rating = model.Rating,
                CreatedAt = DateTime.Now
            };

            _context.Opinions.Add(opinion);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Dziękujemy! Twoja opinia została dodana.";
            return RedirectToAction(nameof(MyOrders));
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }
    }
}