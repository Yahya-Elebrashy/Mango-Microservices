using Mango.Services.CouponAPI.Models.Dto;

namespace Mango.Services.CouponAPI.Services.IServices;

public interface ICouponService
{
    Task<IEnumerable<CouponDto>> GetAllCouponsAsync();
    Task<CouponDto?>             GetCouponByIdAsync(int id);
    Task<CouponDto?>             GetCouponByCodeAsync(string code);
    Task<CouponDto>              CreateCouponAsync(CouponDto couponDto);
    Task                         DeleteCouponAsync(int id);
}
