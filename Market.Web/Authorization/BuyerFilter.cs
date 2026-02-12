using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Market.Web.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Market.Web.Authorization
{
    public class BuyerFilter : IAsyncAuthorizationFilter
    {
        private readonly ApplicationDbContext _context;

        public BuyerFilter(ApplicationDbContext context)
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

            var isProfileComplete = await _context.UserProfiles
                .AnyAsync(x => x.UserId == userId 
                               && !string.IsNullOrEmpty(x.FirstName) 
                               && !string.IsNullOrEmpty(x.LastName));

            if (!isProfileComplete)
            {
                context.Result = new RedirectToActionResult("EditProfile", "Profile", null);
            }
        }
    }
}