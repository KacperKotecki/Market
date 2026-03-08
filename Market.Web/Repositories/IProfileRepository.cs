using Market.Web.Core.Models;

namespace Market.Web.Repositories;

public interface IProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(string userId);
    Task<bool> HasCompleteBasicProfileReadOnlyAsync(string userId);
    Task<bool> HasIbanInProfileReadOnlyAsync(string userId);
    Task AddAsync(UserProfile profile);
    void RemoveCompanyProfile(CompanyProfile companyProfile);
}