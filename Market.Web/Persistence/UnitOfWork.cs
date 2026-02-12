using Microsoft.AspNetCore.Identity;
using Market.Web.Core.Models;
using Market.Web.Repositories;

namespace Market.Web.Persistence.Data;

public class UnitOfWork
{
    private readonly ApplicationDbContext _context;
    public AuctionRepository Auctions { get; }
    public OrderRepository Orders { get; }
    public ProfileRepository Profiles { get; }
    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
      
    }
    public void Complete()
    {
        _context.SaveChanges();
    }
}