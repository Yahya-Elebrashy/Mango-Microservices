using Mango.Services.CouponAPI.Data;
using Mango.Services.CouponAPI.Models;
using Mango.Services.CouponAPI.Repositories.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.CouponAPI.Repositories;

public class CouponRepository : Repository<Coupon>, ICouponRepository
{
    private readonly AppDbContext _db;

    public CouponRepository(AppDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<Coupon?> GetByCodeAsync(string code) =>
        await _db.Coupons.FirstOrDefaultAsync(c => c.CouponCode.ToLower() == code.ToLower());
}
