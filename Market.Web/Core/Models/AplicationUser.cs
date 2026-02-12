using Market.Web.Core.Models;
using Microsoft.AspNetCore.Identity;


namespace Market.Web.Core.Models;

public class ApplicationUser : IdentityUser
{
    public virtual UserProfile? UserProfile { get; set; }

    public bool IsBlocked { get; set; } = false;

    public string? BlockReason { get; set; }
}    
    
