using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Repositories;
using Mango.Services.ShoppingCartAPI.Repositories.IRepository;

namespace Mango.Services.ShoppingCartAPI.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public ICartHeaderRepository  CartHeader  { get; private set; }
    public ICartDetailsRepository CartDetails { get; private set; }

    public UnitOfWork(AppDbContext db)
    {
        _db         = db;
        CartHeader  = new CartHeaderRepository(db);
        CartDetails = new CartDetailsRepository(db);
    }

    public async Task SaveAsync()
    {
        await _db.SaveChangesAsync();
    }
}
