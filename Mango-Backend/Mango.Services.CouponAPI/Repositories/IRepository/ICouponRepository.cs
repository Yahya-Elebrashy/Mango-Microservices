using Mango.Services.CouponAPI.Models;

namespace Mango.Services.CouponAPI.Repositories.IRepository;

public interface ICouponRepository : IRepository<Coupon>
{
    Task<Coupon?> GetByCodeAsync(string code);
}
