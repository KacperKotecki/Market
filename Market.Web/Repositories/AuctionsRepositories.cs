using Market.Web.Data;
using Market.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Market.Web.Repositories;

public class AuctionRepository : IAuctionRepository
{
    private readonly ApplicationDbContext _context;

    public AuctionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Auction>> GetAllAsync()
    {
        return await _context.Auctions
            .Include(x => x.User)
            .Include(x => x.Images) 
            .ToListAsync();
    }
    public async Task<List<Auction>> GetUserAuctionsAsync(string userId)
    {
        return await _context.Auctions
            .Include(a => a.Images)
            .Include(a => a.Orders)
                .ThenInclude(o => o.Opinion)
                    .ThenInclude(op => op.Buyer)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
    public async Task<Auction?> GetByIdAsync(int id)
    {
        return await _context.Auctions
            .Include(x => x.User)
            .Include(x => x.Images) 
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(Auction auction)
    {
        _context.Auctions.Add(auction);
        await _context.SaveChangesAsync(); 
    }
    
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}