using Market.Web.Persistence.Data; // Dla ApplicationDbContext
using Market.Web.Repositories;     // Dla interfejsów i klas repozytoriów

namespace Market.Web.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Auctions = new AuctionRepository(_context);
        Orders = new OrderRepository(_context);
        Profiles = new ProfileRepository(_context);
    }

    public IAuctionRepository Auctions { get; private set; }
    public IOrderRepository Orders { get; private set; }
    public IProfileRepository Profiles { get; private set; }

    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    }

    public async Task CompleteAsync()
    {
        await _context.SaveChangesAsync();
    }
}