using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Market.Web.Models;
using Market.Web.ViewModels;
using Market.Web.Services.Payments;
using Market.Web.Repositories;

namespace Market.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StripePaymentService _stripePaymentService;
        private readonly IAuctionRepository _auctionRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IOrderRepository _orderRepository; 

        public OrderController(
            UserManager<ApplicationUser> userManager, 
            StripePaymentService stripePaymentService,
            IAuctionRepository auctionRepository,
            IProfileRepository profileRepository,
            IOrderRepository orderRepository) 
        {
            _userManager = userManager;
            _stripePaymentService = stripePaymentService;
            _auctionRepository = auctionRepository;
            _profileRepository = profileRepository;
            _orderRepository = orderRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int auctionId)
        {
            var auction = await _auctionRepository.GetByIdAsync(auctionId);

            if (auction == null || auction.Quantity <= 0 || auction.EndDate <= DateTime.Now)
            {
                return RedirectToAction("Index", "Auctions");
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (auction.UserId == user.Id)
            {
                 TempData["Error"] = "Nie możesz kupić własnego przedmiotu.";
                 return RedirectToAction("Details", "Auctions", new { id = auction.Id });
            }

            var userProfile = await _profileRepository.GetByUserIdAsync(user.Id);

            var model = new CheckoutViewModel
            {
                AuctionId = auction.Id,
                AuctionTitle = auction.Title,
                Price = auction.Price,
                IsCompanySale = auction.IsCompanySale,
                SellerName = auction.User?.UserName ?? "Nieznany",
                ImageUrl = auction.Images.FirstOrDefault()?.ImagePath ?? "/img/placeholder.png",
                BuyerName = userProfile != null ? $"{userProfile.FirstName} {userProfile.LastName}" : user.Email!,
                ShippingAddress = userProfile?.ShippingAddress ?? new Address(),
                BuyerHasCompanyProfile = userProfile?.CompanyProfile != null,
                BuyerCompanyName = userProfile?.CompanyProfile?.CompanyName,
                BuyerNIP = userProfile?.CompanyProfile?.NIP,
                BuyerInvoiceAddress = userProfile?.CompanyProfile?.InvoiceAddress
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var auction = await _auctionRepository.GetByIdAsync(model.AuctionId);
            
            if (auction == null || auction.Quantity <= 0) 
            {
                TempData["Error"] = "Przedmiot został właśnie sprzedany lub aukcja wygasła.";
                return RedirectToAction("Index", "Auctions");
            }

            var user = await _userManager.GetUserAsync(User);
            var userProfile = await _profileRepository.GetByUserIdAsync(user.Id);

            if (model.WantsInvoice && userProfile?.CompanyProfile == null)
            {
                ModelState.AddModelError("", "Brak danych firmy do faktury.");
                return View("Checkout", model); 
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
                order.BuyerInvoiceAddress = $"{cp.InvoiceAddress.Street}, {cp.InvoiceAddress.PostalCode} {cp.InvoiceAddress.City}";
            }

            auction.Quantity -= 1;
            if (auction.Quantity == 0) auction.AuctionStatus = AuctionStatus.Sold;

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();
            
            try
            {
                var domain = $"{Request.Scheme}://{Request.Host}";
                var checkoutUrl = await _stripePaymentService.CreateCheckoutSession(order, domain);
                return Redirect(checkoutUrl);
            }
            catch (Exception)
            {
                auction.Quantity += 1;
                if (auction.AuctionStatus == AuctionStatus.Sold) auction.AuctionStatus = AuctionStatus.Active;
                
                _orderRepository.Remove(order);
                await _orderRepository.SaveChangesAsync();

                TempData["Error"] = "Błąd inicjalizacji płatności.";
                return RedirectToAction("Details", "Auctions", new { id = model.AuctionId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int orderId, string session_id)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return NotFound();
            
            return View("OrderConfirmation", order.Id);
        }

        [HttpGet]
        public IActionResult PaymentCancel(int orderId)
        {
            TempData["Error"] = "Płatność została anulowana.";
            return RedirectToAction(nameof(MyOrders));
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var orders = await _orderRepository.GetBuyerOrdersAsync(user.Id);

            var model = orders.Select(o => new MyOrderViewModel
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
            }).ToList();

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MySales()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var sales = await _orderRepository.GetSellerSalesAsync(user.Id);

            var model = sales.Select(o => new MySaleViewModel
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                TotalPrice = o.TotalPrice,
                Status = o.Status,
                AuctionId = o.AuctionId,
                AuctionTitle = o.Auction?.Title ?? "Brak tytułu",
                BuyerName = o.Buyer?.UserName ?? "Nieznany",
                BuyerEmail = o.Buyer?.Email ?? "",
                IsCompanyPurchase = o.IsCompanyPurchase,
                ShippingAddressString = o.Buyer?.UserProfile?.ShippingAddress != null
                        ? $"{o.Buyer.UserProfile.ShippingAddress.Street}, {o.Buyer.UserProfile.ShippingAddress.PostalCode} {o.Buyer.UserProfile.ShippingAddress.City}"
                        : "Brak danych adresowych",
                InvoiceDataString = o.IsCompanyPurchase 
                        ? $"{o.BuyerCompanyName} (NIP: {o.BuyerNIP})\n{o.BuyerInvoiceAddress}" 
                        : null
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null) return NotFound();
            if (order.Auction == null || order.Auction.UserId != user.Id) return Forbid();

            order.Status = newStatus;
            
            await _orderRepository.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Zmieniono status zamówienia #{order.Id} na {newStatus}.";
            return RedirectToAction(nameof(MySales));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelivery(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null) return NotFound();
            if (order.BuyerId != user.Id) return Forbid();

            if (order.Status != OrderStatus.Shipped)
            {
                 TempData["Error"] = "Możesz potwierdzić odbiór tylko wysłanych zamówień.";
                 return RedirectToAction(nameof(MyOrders));
            }

            order.Status = OrderStatus.Completed;

            if(order.Auction?.User?.UserProfile != null)
            {
                order.Auction.User.UserProfile.WalletBalance += order.TotalPrice;
            }
            
            await _orderRepository.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Transakcja zakończona pomyślnie.";
            return RedirectToAction(nameof(MyOrders));
        }

        [HttpGet]
        public async Task<IActionResult> Rate(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _orderRepository.GetByIdAsync(id);

            if (order == null) return NotFound();
            if (order.BuyerId != user.Id) return Forbid();
            if (order.Status == OrderStatus.Pending) return BadRequest("Zamówienie nieopłacone.");
            if (order.Opinion != null) return BadRequest("Już oceniono.");

            var model = new RateOrderViewModel
            {
                OrderId = order.Id,
                AuctionTitle = order.Auction?.Title ?? "Usunięta",
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
            var order = await _orderRepository.GetByIdAsync(model.OrderId); // ZMIANA

            if (order == null || order.BuyerId != user.Id) return Forbid();
            if (order.Opinion != null) return BadRequest("Już oceniono.");

            var opinion = new Opinion
            {
                OrderId = order.Id,
                BuyerId = user.Id,
                SellerId = order.Auction!.UserId,
                Comment = model.Comment,
                Rating = model.Rating,
                CreatedAt = DateTime.Now
            };

            await _orderRepository.AddOpinionAsync(opinion);
            await _orderRepository.SaveChangesAsync();

            TempData["SuccessMessage"] = "Opinię dodano pomyślnie.";
            return RedirectToAction(nameof(MyOrders));
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }
    }
}