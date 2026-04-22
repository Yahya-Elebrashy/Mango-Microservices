using Mango.Services.CouponAPI.Repositories.IRepository;

namespace Mango.Services.CouponAPI.UnitOfWork;

public interface IUnitOfWork
{
    ICouponRepository Coupon { get; }
    Task SaveAsync();
}
