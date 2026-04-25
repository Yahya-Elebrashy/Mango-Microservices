using AutoMapper;
using Mango.Services.CouponAPI.Models;
using Mango.Services.CouponAPI.Models.Dto;
using Mango.Services.CouponAPI.Services.IServices;
using Mango.Services.CouponAPI.UnitOfWork;

namespace Mango.Services.CouponAPI.Services;

public class CouponService : ICouponService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CouponService> _logger;

    public CouponService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CouponService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<CouponDto>> GetAllCouponsAsync()
    {
        _logger.LogInformation("Fetching all coupons");

        var coupons = await _unitOfWork.Coupon.GetAllAsync();
        var result = _mapper.Map<IEnumerable<CouponDto>>(coupons);

        _logger.LogInformation("Successfully retrieved {Count} coupons", result.Count());
        return result;
    }

    public async Task<CouponDto?> GetCouponByIdAsync(int id)
    {
        _logger.LogInformation("Fetching coupon with ID {CouponId}", id);

        var coupon = await _unitOfWork.Coupon.GetAsync(c => c.CouponId == id);

        if (coupon == null)
        {
            _logger.LogWarning("Coupon with ID {CouponId} was not found", id);
            return null;
        }

        _logger.LogInformation("Successfully retrieved coupon with ID {CouponId}", id);
        return _mapper.Map<CouponDto>(coupon);
    }

    public async Task<CouponDto?> GetCouponByCodeAsync(string code)
    {
        _logger.LogInformation("Fetching coupon with code {CouponCode}", code);

        var coupon = await _unitOfWork.Coupon.GetByCodeAsync(code);

        if (coupon == null)
        {
            _logger.LogWarning("Coupon with code {CouponCode} was not found", code);
            return null;
        }

        _logger.LogInformation("Successfully retrieved coupon with code {CouponCode} (ID={CouponId})",
            code, coupon.CouponId);
        return _mapper.Map<CouponDto>(coupon);
    }

    public async Task<CouponDto> CreateCouponAsync(CouponDto couponDto)
    {
        _logger.LogInformation("Creating coupon. Code={CouponCode}, DiscountAmount={DiscountAmount}",
            couponDto.CouponCode, couponDto.DiscountAmount);

        var coupon = _mapper.Map<Coupon>(couponDto);

        await _unitOfWork.Coupon.CreateAsync(coupon);
        await _unitOfWork.SaveAsync();

        _logger.LogInformation("Coupon saved to DB. Code={CouponCode}, ID={CouponId}",
            coupon.CouponCode, coupon.CouponId);

        await TryCreateStripeCouponAsync(couponDto);

        _logger.LogInformation("Coupon {CouponCode} (ID={CouponId}) created successfully",
            coupon.CouponCode, coupon.CouponId);
        return _mapper.Map<CouponDto>(coupon);
    }

    public async Task DeleteCouponAsync(int id)
    {
        _logger.LogInformation("Deleting coupon with ID {CouponId}", id);

        var coupon = await _unitOfWork.Coupon.GetAsync(c => c.CouponId == id);

        if (coupon == null)
        {
            _logger.LogWarning("Delete failed — coupon with ID {CouponId} not found", id);
            throw new KeyNotFoundException("Coupon not found");
        }

        await _unitOfWork.Coupon.RemoveAsync(coupon);
        await _unitOfWork.SaveAsync();

        _logger.LogInformation("Coupon {CouponCode} (ID={CouponId}) removed from DB",
            coupon.CouponCode, coupon.CouponId);

        await TryDeleteStripeCouponAsync(coupon.CouponCode);

        _logger.LogInformation("Coupon {CouponCode} (ID={CouponId}) deleted successfully",
            coupon.CouponCode, coupon.CouponId);
    }

    // ─── Stripe Helpers ──────────────────────────────────────────────────────────

    private async Task TryCreateStripeCouponAsync(CouponDto couponDto)
    {
        _logger.LogInformation("Attempting to create Stripe coupon for code {CouponCode}", couponDto.CouponCode);
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
            await Task.Run(() => service.Create(options));

            _logger.LogInformation("Stripe coupon created successfully for code {CouponCode}", couponDto.CouponCode);
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex,
                "Stripe error while creating coupon {CouponCode}. StripeErrorCode={StripeErrorCode}, Message={Message}",
                couponDto.CouponCode, ex.StripeError?.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while creating Stripe coupon for code {CouponCode}",
                couponDto.CouponCode);
        }
    }

    private async Task TryDeleteStripeCouponAsync(string couponCode)
    {
        _logger.LogInformation("Attempting to delete Stripe coupon with code {CouponCode}", couponCode);
        try
        {
            var service = new Stripe.CouponService();
            await Task.Run(() => service.Delete(couponCode));

            _logger.LogInformation("Stripe coupon deleted successfully for code {CouponCode}", couponCode);
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex,
                "Stripe error while deleting coupon {CouponCode}. StripeErrorCode={StripeErrorCode}, Message={Message}",
                couponCode, ex.StripeError?.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while deleting Stripe coupon for code {CouponCode}",
                couponCode);
        }
    }
}