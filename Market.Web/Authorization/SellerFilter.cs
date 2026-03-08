using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Market.Web.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Market.Web.Authorization;

public class SellerFilter : IAsyncAuthorizationFilter
{
    private readonly IProfileService _profileService;

    public SellerFilter(IProfileService profileService)
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
        
        bool hasBasicInfo = await _profileService.HasCompleteBasicProfileReadOnlyAsync(userId!);

        bool hasIban = await _profileService.HasIbanInProfileReadOnlyAsync(userId!);
        if (!hasBasicInfo || !hasIban)
        {
            if (context.HttpContext.RequestServices.GetService(typeof(ITempDataDictionaryFactory)) is ITempDataDictionaryFactory factory)
            {
                var tempData = factory.GetTempData(context.HttpContext);
                tempData["WarningMessage"] = "Aby sprzedawać, musisz uzupełnić dane profilowe oraz numer IBAN.";
            }

            context.Result = new RedirectToActionResult("EditProfile", "Profile", null);
        }
    }
}
