using Market.Web.Core.Models;
using Market.Web.Repositories;

namespace Market.Web.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProfileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<UserProfile?> GetByUserIdAsync(string userId)
    {
        return await _unitOfWork.Profiles.GetByUserIdAsync(userId);
    }
    
}