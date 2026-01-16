using Microsoft.AspNetCore.Identity;

namespace Market.Web.Models;

public class ApplicationUser : IdentityUser
{
    public virtual UserProfile? UserProfile { get; set; }

    public bool IsBlocked { get; set; } = false;
}    
    
