using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Market.Web.Core.Models;
using Market.Web.Services;

namespace Market.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhookController> _logger;
        private readonly IOrderService _orderService;

        public WebhookController(
            IConfiguration configuration, 
            ILogger<WebhookController> logger, 
            IOrderService orderService)
        {
            _configuration = configuration;
            _logger = logger;
            _orderService = orderService;
        }

        [HttpPost("stripe")]
        public async Task<IActionResult> Index()
        {
        #pragma warning disable CA1869 // Do not cache instances of 'StreamReader'. False positive for Stripe raw usage.
            using var reader = new StreamReader(HttpContext.Request.Body);
            var json = await reader.ReadToEndAsync();
        #pragma warning restore CA1869
            var endpointSecret = _configuration["Stripe:WebhookSecret"];

            if (string.IsNullOrEmpty(endpointSecret))
            {
                 _logger.LogError("BŁĄD KONFIGURACJI: Brak Stripe:WebhookSecret w ustawieniach.");
                 return BadRequest();
            }

            Stripe.Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], endpointSecret);
                _logger.LogInformation("Podpis zweryfikowany. Typ zdarzenia: {Type}", stripeEvent.Type);
            }
            catch (Exception e)
            {
                _logger.LogError("BŁĄD WERYFIKACJI PODPISU: {Message}", e.Message);
                return BadRequest();
            }

            if (stripeEvent.Type != "checkout.session.completed")
            {
                return Ok();
            }

            var session = stripeEvent.Data.Object as Session;
            
            _logger.LogInformation("Przetwarzanie sesji. Session ID: {Id}", session?.Id);

            if (session == null || !session.Metadata.TryGetValue("OrderId", out string? orderIdStr))
            {
                 _logger.LogWarning("BŁĄD: Brak OrderId w metadanych sesji!");
                 return Ok(); 
            }

            if (!int.TryParse(orderIdStr, out int orderId))
            {
                return Ok();
            }

            await _orderService.MarkOrderAsPaidAsync(orderId);
            _logger.LogInformation($"Zlecono oznaczenie zamówienia {orderId} jako opłacone.");

            return Ok();
        }
    }
}