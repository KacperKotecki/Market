using Market.Web.Models;

namespace Market.Web.Repositories;

public interface IProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(string userId);
    Task AddAsync(UserProfile profile);
    void RemoveCompanyProfile(CompanyProfile companyProfile);
    Task SaveChangesAsync();
}