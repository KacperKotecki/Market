using Market.Web.Data;
using Market.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Market.Web.Repositories;

public class ProfileRepository : IProfileRepository
{
    private readonly ApplicationDbContext _context;

    public ProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByUserIdAsync(string userId)
    {
        return await _context.UserProfiles
            .Include(p => p.CompanyProfile)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task AddAsync(UserProfile profile)
    {
        await _context.UserProfiles.AddAsync(profile);
    }

    public void RemoveCompanyProfile(CompanyProfile companyProfile)
    {
        _context.CompanyProfiles.Remove(companyProfile);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}