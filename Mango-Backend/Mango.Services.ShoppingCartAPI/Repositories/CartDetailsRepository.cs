using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Repositories.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ShoppingCartAPI.Repositories;

public class CartDetailsRepository : Repository<CartDetails>, ICartDetailsRepository
{
    private readonly AppDbContext _db;

    public CartDetailsRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<int> CountByHeaderIdAsync(int cartHeaderId) =>
        await _db.CartDetails.CountAsync(c => c.CartHeaderId == cartHeaderId);
}
