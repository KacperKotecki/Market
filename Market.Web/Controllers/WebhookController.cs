using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Market.Web.Persistence.Data;
using Market.Web.Core.Models;
using Microsoft.EntityFrameworkCore;
using Stripe.V2.Core;
using Market.Web.Repositories;

namespace Market.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<WebhookController> logger)
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [HttpPost("stripe")]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
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

            if (session == null || !session.Metadata.TryGetValue("OrderId", out string orderIdStr))
            {
                 _logger.LogWarning("BŁĄD: Brak OrderId w metadanych sesji!");
                 return Ok(); 
            }

            if (!int.TryParse(orderIdStr, out int orderId))
            {
                return Ok();
            }

            await MarkOrderAsPaid(orderId);

            return Ok();
        }


        private async Task MarkOrderAsPaid(int orderId)
        {
        
            using (var scope = _scopeFactory.CreateScope())
            {

                var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                
                var order = await orderRepo.GetByIdAsync(orderId);

                if (order == null)
                {
                    _logger.LogError($"Zamówienie {orderId} nie znalezione.");
                    return;
                }

                if (order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Paid;

                    await orderRepo.SaveChangesAsync();
                    
                    _logger.LogInformation($"Zamówienie {orderId} opłacone pomyślnie.");
                }
            }
        }
    }
}