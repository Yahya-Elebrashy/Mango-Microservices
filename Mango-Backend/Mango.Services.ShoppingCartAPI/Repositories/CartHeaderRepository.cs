using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Repositories.IRepository;

namespace Mango.Services.ShoppingCartAPI.Repositories;

public class CartHeaderRepository : Repository<CartHeader>, ICartHeaderRepository
{
    public CartHeaderRepository(AppDbContext db) : base(db) { }
}
