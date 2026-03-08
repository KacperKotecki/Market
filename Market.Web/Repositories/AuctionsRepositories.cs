using Market.Web.Persistence.Data;
using Market.Web.Core.DTOs;
using Market.Web.Core.Models;
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

    public async Task<(List<Auction> Items, int TotalCount)> GetAllWithFiltersAsync(AuctionFilter filter)
    {
        var query = _context.Auctions
            .Include(a => a.User)
            .Include(a => a.Images)
            .AsQueryable();

        var targetStatus = filter.Status ?? AuctionStatus.Active;
        query = query.Where(a => a.AuctionStatus == targetStatus && a.EndDate > DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(filter.SearchString))
        {
            string search = filter.SearchString.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(search) ||
                a.Description.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(a => a.Category == filter.Category);

        if (filter.MinPrice.HasValue)
            query = query.Where(a => a.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(a => a.Price <= filter.MaxPrice.Value);

        query = filter.SortOrder switch
        {
            "price_desc" => query.OrderByDescending(a => a.Price),
            "price_asc"  => query.OrderBy(a => a.Price),
            _            => query.OrderByDescending(a => a.CreatedAt),
        };

        int totalCount = await query.CountAsync();

        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Auction auction)
    {
        _context.Auctions.Add(auction);
    }
    public async Task UpdateAsync(Auction auction)
    {
        _context.Auctions.Update(auction);
    }
}