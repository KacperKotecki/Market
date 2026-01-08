using Microsoft.EntityFrameworkCore;
using Market.Web.Data;
using Market.Web.Models;

namespace Market.Web.Repositories;

public class AuctionRepository : IAuctionRepository
{
    private readonly ApplicationDbContext _context;
    

    public AuctionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // --- YOUR LOGIC IS HERE ---
    public async Task<List<Auction>> GetAllAsync()
    {
        // We moved this exact code from the Controller to here
        return await _context.Auctions
            .Include(a => a.User) 
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Auction?> GetByIdAsync(int id)
    {
        return await _context.Auctions
            .Include(a => a.User)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task AddAsync(Auction auction)
    {
        await _context.Auctions.AddAsync(auction);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}