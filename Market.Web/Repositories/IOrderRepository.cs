using Market.Web.Core.Models;

namespace Market.Web.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<List<Order>> GetBuyerOrdersAsync(string buyerId);
    Task<List<Order>> GetSellerSalesAsync(string sellerId);
    void Remove(Order order);
    Task<Order?> GetByIdAsync(int id);
    Task AddOpinionAsync(Opinion opinion);
    Task SaveChangesAsync();
}