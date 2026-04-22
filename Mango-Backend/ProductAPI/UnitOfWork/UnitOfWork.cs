using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Repositories;
using Mango.Services.ProductAPI.Repositories.IRepository;

namespace Mango.Services.ProductAPI.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public IProductRepository Product { get; private set; }

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Product = new ProductRepository(db);
    }

    public async Task SaveAsync()
    {
        await _db.SaveChangesAsync();
    }
}
