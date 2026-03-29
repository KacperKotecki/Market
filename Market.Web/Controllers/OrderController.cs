using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Market.Web.Core.Exceptions;
using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;
using Market.Web.Services;
using Market.Web.Core.DTOs;

namespace Market.Web.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        UserManager<ApplicationUser> userManager,
        IOrderService orderService,
        ILogger<OrderController> logger)
    {
        _userManager = userManager;
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(int auctionId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var model = await _orderService.GetCheckoutModelAsync(auctionId, user.Id);
        
        if (model == null)
        {
             TempData["Error"] = "Aukcja niedostępna lub próba zakupu własnego przedmiotu.";
             return RedirectToAction("Index", "Auctions");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var model = await _orderService.GetBuyerOrdersAsync(user.Id);
        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MySales()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var model = await _orderService.GetSellerSalesAsync(user.Id);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        if (!ModelState.IsValid) return View("Checkout", model);

        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        try
        {
            var domain = $"{Request.Scheme}://{Request.Host}";
            var checkoutUrl = await _orderService.PlaceOrderAsync(model, user.Id, domain);
            return Redirect(checkoutUrl);
        }
        catch (StockUnavailableException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", "Auctions", new { id = model.AuctionId });
        }
        catch (ConcurrencyException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Details", "Auctions", new { id = model.AuctionId });
        }
        catch (ArgumentException ex) 
        {
            ModelState.AddModelError("", ex.Message);
            return View("Checkout", model);
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Unexpected error placing order for AuctionId {AuctionId}, UserId {UserId}",
                model.AuctionId, user.Id);
            TempData["Error"] = "Wystąpił nieoczekiwany błąd. Spróbuj ponownie.";
            return RedirectToAction("Details", "Auctions", new { id = model.AuctionId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus)
    {
        if (!ModelState.IsValid) return BadRequest();

        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        try 
        {
            await _orderService.UpdateOrderStatusAsync(orderId, newStatus, user.Id);
            TempData["SuccessMessage"] = $"Zmieniono status na {newStatus}.";
        }
        catch (OrderAuthorizationException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating order status OrderId {OrderId}, UserId {UserId}",
                orderId, user.Id);
            TempData["Error"] = "Wystąpił nieoczekiwany błąd. Spróbuj ponownie.";
        }

        return RedirectToAction(nameof(MySales));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDelivery(int orderId)
    {
        if (!ModelState.IsValid) return BadRequest();

        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        try
        {
            await _orderService.ConfirmDeliveryAsync(orderId, user.Id);
            TempData["SuccessMessage"] = "Transakcja zakończona pomyślnie.";
        }
        catch (ConcurrencyException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error confirming delivery OrderId {OrderId}, UserId {UserId}",
                orderId, user.Id);
            TempData["Error"] = "Wystąpił nieoczekiwany błąd. Spróbuj ponownie.";
        }

        return RedirectToAction(nameof(MyOrders));
    }

    [HttpGet]
    public async Task<IActionResult> Rate(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var model = await _orderService.GetRateOrderModelAsync(id, user.Id);
        
        if (model == null) return NotFound();
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(RateOrderViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        try
        {
            await _orderService.AddOpinionAsync(model, user.Id);
            TempData["SuccessMessage"] = "Opinię dodano pomyślnie.";
            return RedirectToAction(nameof(MyOrders));
        }
        catch (OrderAuthorizationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(MyOrders));
        }
        catch (OpinionAlreadyExistsException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(MyOrders));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error rating order {OrderId}, UserId {UserId}",
                model.OrderId, user.Id);
            TempData["Error"] = "Wystąpił nieoczekiwany błąd. Spróbuj ponownie.";
            return RedirectToAction(nameof(MyOrders));
        }
    }

    [HttpGet]
    public async Task<IActionResult> PaymentSuccess(int orderId, string session_id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();
        

        try
        {
            var orderDetailDto = await _orderService.GetOrderByIdAsync(orderId, user.Id);
            if (orderDetailDto == null) return NotFound();
            return View("OrderConfirmation", orderDetailDto);
        }
        catch (OrderAuthorizationException)
        {
            return Forbid();
        }
    }

    [HttpGet]
    public IActionResult PaymentCancel(int orderId)
    {
        TempData["Error"] = "Płatność została anulowana.";
        return RedirectToAction(nameof(MyOrders));
    }

    public async Task<IActionResult> OrderConfirmation(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        try
        {
            var orderDetailDto = await _orderService.GetOrderByIdAsync(id, user.Id);
            if (orderDetailDto == null) return NotFound();
            return View(orderDetailDto);
        }
        catch (OrderAuthorizationException)
        {
            return Forbid();
        }
    }

    private async Task<ApplicationUser> GetCurrentUserAsync() => await _userManager.GetUserAsync(User);

}