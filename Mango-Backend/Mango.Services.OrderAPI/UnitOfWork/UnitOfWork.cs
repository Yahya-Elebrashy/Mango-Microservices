using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Repositories;
using Mango.Services.OrderAPI.Repositories.IRepository;

namespace Mango.Services.OrderAPI.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public IOrderRepository Order { get; private set; }

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Order = new OrderRepository(db);
    }

    public async Task SaveAsync()
    {
        await _db.SaveChangesAsync();
    }
}
