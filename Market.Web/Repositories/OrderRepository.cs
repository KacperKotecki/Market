using Market.Web.Data;
using Market.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Market.Web.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task<List<Order>> GetBuyerOrdersAsync(string buyerId)
    {
        return await _context.Orders
            .Include(o => o.Auction)
                .ThenInclude(a => a.Images)
            .Include(o => o.Auction)
                .ThenInclude(a => a.User)
            .Include(o => o.Opinion)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<List<Order>> GetSellerSalesAsync(string sellerId)
    {
        return await _context.Orders
            .Include(o => o.Auction)
            .Include(o => o.Buyer)
            .Where(o => o.Auction.UserId == sellerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
    public void Remove(Order order)
    {
        _context.Orders.Remove(order);
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Auction)
                .ThenInclude(a => a.User)
                    .ThenInclude(u => u.UserProfile)
            .Include(o => o.Auction)
                .ThenInclude(a => a.Images)
            .Include(o => o.Opinion)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task AddOpinionAsync(Opinion opinion)
    {
        await _context.Opinions.AddAsync(opinion);
    }
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}