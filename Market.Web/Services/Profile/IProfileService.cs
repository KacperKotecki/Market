using Market.Web.Core.Models;

namespace Market.Web.Services;

public interface IProfileService
{
    Task<UserProfile?> GetByUserIdAsync(string userId);
}