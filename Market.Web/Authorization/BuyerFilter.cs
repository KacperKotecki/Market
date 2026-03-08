using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Market.Web.Services;
using System.Security.Claims;

namespace Market.Web.Authorization;

public class BuyerFilter : IAsyncAuthorizationFilter
{
    private readonly IProfileService _profileService;

    public BuyerFilter(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        bool isProfileComplete = await _profileService.HasCompleteBasicProfileReadOnlyAsync(userId!);

        if (!isProfileComplete)
        {
            context.Result = new RedirectToActionResult("EditProfile", "Profile", null);
        }
    }
}
