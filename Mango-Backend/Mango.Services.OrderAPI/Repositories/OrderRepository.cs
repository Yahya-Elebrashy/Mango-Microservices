using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Repositories.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.OrderAPI.Repositories;

public class OrderRepository : Repository<OrderHeader>, IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<OrderHeader>> GetAllWithDetailsAsync() =>
        await _db.OrderHeaders
            .Include(o => o.OrderDetails)
            .OrderByDescending(o => o.OrderHeaderId)
            .ToListAsync();

    public async Task<OrderHeader?> GetWithDetailsAsync(int orderHeaderId) =>
        await _db.OrderHeaders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderHeaderId == orderHeaderId);

    public async Task<IEnumerable<OrderHeader>> GetByUserIdAsync(string userId) =>
        await _db.OrderHeaders
            .Include(o => o.OrderDetails)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderHeaderId)
            .ToListAsync();
}
