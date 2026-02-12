using Market.Web.Core.Models;

namespace Market.Web.Services;

public interface IPaymentService
{
    Task<string> CreateCheckoutSession(Order order, string domain);
};