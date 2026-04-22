using Mango.Services.CouponAPI.Data;
using Mango.Services.CouponAPI.Repositories;
using Mango.Services.CouponAPI.Repositories.IRepository;

namespace Mango.Services.CouponAPI.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public ICouponRepository Coupon { get; private set; }

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Coupon = new CouponRepository(db);
    }

    public async Task SaveAsync()
    {
        await _db.SaveChangesAsync();
    }
}
