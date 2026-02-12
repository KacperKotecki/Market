using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Market.Web.Core.Models;

namespace Market.Web.Persistence.Data;

// Pamiętaj o generycznym <ApplicationUser>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Auction> Auctions { get; set; } 
    
    public DbSet<AuctionImage> AuctionImages { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<CompanyProfile> CompanyProfiles { get; set; }

    public DbSet<Order> Orders { get; set; } 

    public DbSet<Opinion> Opinions { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.UserProfile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade); 


        builder.Entity<UserProfile>()
            .HasOne(p => p.CompanyProfile)
            .WithOne(c => c.UserProfile)
            .HasForeignKey<CompanyProfile>(c => c.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade); 

        
        builder.Entity<UserProfile>()
            .OwnsOne(x => x.ShippingAddress);

        builder.Entity<CompanyProfile>()
            .OwnsOne(x => x.InvoiceAddress);
    }
}