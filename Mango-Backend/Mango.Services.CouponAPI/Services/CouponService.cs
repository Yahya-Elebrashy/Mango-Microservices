using AutoMapper;
using Mango.Services.CouponAPI.Models;
using Mango.Services.CouponAPI.Models.Dto;
using Mango.Services.CouponAPI.Services.IServices;
using Mango.Services.CouponAPI.UnitOfWork;

namespace Mango.Services.CouponAPI.Services;

public class CouponService : ICouponService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public CouponService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<IEnumerable<CouponDto>> GetAllCouponsAsync()
    {
        var coupons = await _unitOfWork.Coupon.GetAllAsync();
        return _mapper.Map<IEnumerable<CouponDto>>(coupons);
    }

    public async Task<CouponDto?> GetCouponByIdAsync(int id)
    {
        var coupon = await _unitOfWork.Coupon.GetAsync(c => c.CouponId == id);
        return coupon == null ? null : _mapper.Map<CouponDto>(coupon);
    }

    public async Task<CouponDto?> GetCouponByCodeAsync(string code)
    {
        var coupon = await _unitOfWork.Coupon.GetByCodeAsync(code);
        return coupon == null ? null : _mapper.Map<CouponDto>(coupon);
    }

    public async Task<CouponDto> CreateCouponAsync(CouponDto couponDto)
    {
        var coupon = _mapper.Map<Coupon>(couponDto);

        await _unitOfWork.Coupon.CreateAsync(coupon);
        await _unitOfWork.SaveAsync();

        try
        {
            var options = new Stripe.CouponCreateOptions
            {
                AmountOff = (long)(couponDto.DiscountAmount * 100),
                Name = couponDto.CouponCode,
                Currency = "usd",
                Id = couponDto.CouponCode,
            };
            var service = new Stripe.CouponService();
            service.Create(options);
        }
        catch
        {
            // log only — don't break the operation
        }

        return _mapper.Map<CouponDto>(coupon);
    }

    public async Task DeleteCouponAsync(int id)
    {
        var coupon = await _unitOfWork.Coupon.GetAsync(c => c.CouponId == id)
            ?? throw new KeyNotFoundException("Coupon not found");

        await _unitOfWork.Coupon.RemoveAsync(coupon);
        await _unitOfWork.SaveAsync();

        try
        {
            var service = new Stripe.CouponService();
            service.Delete(coupon.CouponCode);
        }
        catch
        {
            // log only — don't break the operation
        }
    }
}
