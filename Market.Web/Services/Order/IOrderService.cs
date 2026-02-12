using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;

namespace Market.Web.Services;

public interface IOrderService
{
    Task<CheckoutViewModel?> GetCheckoutModelAsync(int auctionId, string userId);
    Task<List<MyOrderViewModel>> GetBuyerOrdersAsync(string userId);
    Task<List<MySaleViewModel>> GetSellerSalesAsync(string userId);
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<RateOrderViewModel?> GetRateOrderModelAsync(int orderId, string userId);


    Task<string> PlaceOrderAsync(CheckoutViewModel model, string userId, string domain);
    Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string sellerId);
    Task ConfirmDeliveryAsync(int orderId, string buyerId);
    Task AddOpinionAsync(RateOrderViewModel model, string buyerId);
}