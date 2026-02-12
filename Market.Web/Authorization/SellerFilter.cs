using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Market.Web.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Market.Web.Authorization
{
    public class SellerFilter : IAsyncAuthorizationFilter
    {
        private readonly ApplicationDbContext _context;

        public SellerFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity.IsAuthenticated)
            {
                return; 
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            var userProfile = await _context.UserProfiles
                .Include(p => p.CompanyProfile)
                .AsNoTracking() 
                .FirstOrDefaultAsync(x => x.UserId == userId);


            bool hasBasicInfo = userProfile != null 
                                && !string.IsNullOrEmpty(userProfile.FirstName) 
                                && !string.IsNullOrEmpty(userProfile.LastName);

            bool hasIban = userProfile != null && (
                !string.IsNullOrEmpty(userProfile.PrivateIBAN) || 
                (userProfile.CompanyProfile != null && !string.IsNullOrEmpty(userProfile.CompanyProfile.CompanyIBAN))
            );

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
}