using Market.Web.Data;
using Market.Web.Models.Admin;
using Microsoft.EntityFrameworkCore;
using Market.Web.ViewModels;
using Market.Web.Models;

namespace Market.Web.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UsersListViewModel> GetUsersAsync(string searchString, string sortOrder, int pageNumber, int pageSize)
    {

        var query = _context.Users
            .Include(u => u.UserProfile)
            .ThenInclude(p => p.CompanyProfile)
            .Include(u => u.UserProfile) 
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            searchString = searchString.ToLower();
            query = query.Where(u => 
                u.Email.ToLower().Contains(searchString) ||
                (u.UserProfile != null && (
                    u.UserProfile.LastName.ToLower().Contains(searchString) || 
                    u.UserProfile.FirstName.ToLower().Contains(searchString) ||
                    (u.UserProfile.CompanyProfile != null && u.UserProfile.CompanyProfile.CompanyName.ToLower().Contains(searchString))
                ))
            );
        }

        query = sortOrder switch
        {
            "name_desc" => query.OrderByDescending(u => u.UserProfile.LastName),
            "date_desc" => query.OrderByDescending(u => u.Id), // Zakładając, że ID rośnie w czasie (GUID nie zawsze, ale tu uproszczenie)
            _ => query.OrderBy(u => u.UserProfile.LastName) // Domyślne: Nazwisko A-Z
        };

        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        
        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                Email = u.Email,
                IsBlocked = u.IsBlocked,
                FullName = u.UserProfile != null ? $"{u.UserProfile.FirstName} {u.UserProfile.LastName}" : "Brak profilu",
                CompanyName = u.UserProfile != null && u.UserProfile.CompanyProfile != null ? u.UserProfile.CompanyProfile.CompanyName : "-",
                NIP = u.UserProfile != null && u.UserProfile.CompanyProfile != null ? u.UserProfile.CompanyProfile.NIP : "-",
                AuctionCount = _context.Auctions.Count(a => a.UserId == u.Id) 
            })
            .ToListAsync();

        return new UsersListViewModel
        {
            Users = users,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            SearchString = searchString,
            SortOrder = sortOrder
        };
    }

    public async Task ToggleUserBlockStatusAsync(string userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.IsBlocked = !user.IsBlocked;
            await _context.SaveChangesAsync();
        }
    }
    public async Task<AdminAuctionsListViewModel> GetAuctionsAsync(string searchString, string sortOrder, int pageNumber, int pageSize)
    {
        var query = _context.Auctions
            .Include(a => a.User)     
            .Include(a => a.Images)    
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            string search = searchString.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(search) ||
                a.Description.ToLower().Contains(search) ||
                (a.User != null && a.User.Email.ToLower().Contains(search))
            );
        }

        query = sortOrder switch
        {
            "price_desc" => query.OrderByDescending(a => a.Price),
            "price_asc" => query.OrderBy(a => a.Price),
            "status" => query.OrderByDescending(a => a.AuctionStatus),
            _ => query.OrderByDescending(a => a.CreatedAt) 
        };

        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var auctions = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AdminAuctionDto
            {
                Id = a.Id,
                Title = a.Title,
                Price = a.Price,
                Category = a.Category,
                SellerEmail = a.User != null ? a.User.Email : "Nieznany",
                Status = a.AuctionStatus,
                CreatedAt = a.CreatedAt,
                BannedNote = a.BannedNote,
                ImagePaths = a.Images.Select(img => img.ImagePath).ToList()
            })
            .ToListAsync();

        return new AdminAuctionsListViewModel
        {
            Auctions = auctions,
            SearchString = searchString,
            SortOrder = sortOrder,
            CurrentPage = pageNumber,
            TotalPages = totalPages
        };
    }

    public async Task ToggleAuctionBanAsync(int auctionId, string? reason)
{
    var auction = await _context.Auctions.FindAsync(auctionId);
    if (auction == null) return;

    if (auction.AuctionStatus == AuctionStatus.Banned)
    {
        auction.AuctionStatus = AuctionStatus.Active;
        auction.BannedNote = null; 
    }
    else
    {
        auction.AuctionStatus = AuctionStatus.Banned;
        auction.BannedNote = !string.IsNullOrWhiteSpace(reason) 
            ? reason 
            : "Zablokowane przez administratora (bez podania przyczyny).";
    }

    await _context.SaveChangesAsync();
}
}