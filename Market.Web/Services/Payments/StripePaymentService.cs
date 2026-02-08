using Market.Web.Models;
using Stripe;
using Stripe.Checkout;

namespace Market.Web.Services.Payments
{
    public class StripePaymentService
    {
        private readonly IConfiguration _configuration;

        public StripePaymentService(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<string> CreateCheckoutSession(Order order, string domain)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card", "blik" }, 
                
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(order.TotalPrice * 100), 
                            Currency = "pln",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Zamówienie nr {order.Id}",
                                Description = "Zakup w Market.Web"
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                
                SuccessUrl = $"{domain}/Order/PaymentSuccess?orderId={order.Id}&session_id={{CHECKOUT_SESSION_ID}}",
                
                CancelUrl = $"{domain}/Order/PaymentCancel?orderId={order.Id}",
                
                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", order.Id.ToString() } 
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            return session.Url;
        }
    }
}