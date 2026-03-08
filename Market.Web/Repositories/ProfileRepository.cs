using Market.Web.Persistence.Data;
using Market.Web.Core.Models;
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

    public async Task<bool> HasCompleteBasicProfileReadOnlyAsync(string userId)
    {
        return await _context.UserProfiles
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId
                           && !string.IsNullOrEmpty(x.FirstName)
                           && !string.IsNullOrEmpty(x.LastName)
                           && !string.IsNullOrEmpty(x.ShippingAddress.Street)
                           && !string.IsNullOrEmpty(x.ShippingAddress.City)
                           && !string.IsNullOrEmpty(x.ShippingAddress.PostalCode)
                           && !string.IsNullOrEmpty(x.ShippingAddress.Country));
    }

    public async Task<bool> HasIbanInProfileReadOnlyAsync(string userId)
    {


        return await _context.UserProfiles
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId
               && (!string.IsNullOrEmpty(x.PrivateIBAN)
                   || (x.CompanyProfile != null 
                       && !string.IsNullOrEmpty(x.CompanyProfile.CompanyIBAN))));
    }
    public async Task AddAsync(UserProfile profile)
    {
        await _context.UserProfiles.AddAsync(profile);
    }

    public void RemoveCompanyProfile(CompanyProfile companyProfile)
    {
        _context.CompanyProfiles.Remove(companyProfile);
    }
}