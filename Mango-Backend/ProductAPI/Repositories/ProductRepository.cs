using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Repositories.IRepository;
using ProductAPI.Models;

namespace Mango.Services.ProductAPI.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }
}
