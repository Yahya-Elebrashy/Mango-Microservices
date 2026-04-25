using Mango.Services.CouponAPI.Constants;
using Mango.Services.CouponAPI.Models.Dto;
using Mango.Services.CouponAPI.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.CouponAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;
        private readonly ILogger<CouponController> _logger;

        public CouponController(ICouponService couponService, ILogger<CouponController> logger)
        {
            _couponService = couponService;
            _logger = logger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("GET /api/coupon — Retrieving all coupons");

            var coupons = await _couponService.GetAllCouponsAsync();

            _logger.LogInformation("GET /api/coupon — Returned {Count} coupons", coupons.Count());
            return Ok(new ResponseDto<IEnumerable<CouponDto>> { Result = coupons });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            _logger.LogInformation("GET /api/coupon/{CouponId} — Retrieving coupon", id);

            var coupon = await _couponService.GetCouponByIdAsync(id);

            if (coupon == null)
            {
                _logger.LogWarning("GET /api/coupon/{CouponId} — Coupon not found", id);
                return NotFound(new ResponseDto<CouponDto> { IsSuccess = false, Message = "Coupon not found" });
            }

            _logger.LogInformation("GET /api/coupon/{CouponId} — Coupon found and returned", id);
            return Ok(new ResponseDto<CouponDto> { Result = coupon });
        }

        [HttpGet("GetByCode/{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            _logger.LogInformation("GET /api/coupon/GetByCode/{CouponCode} — Retrieving coupon by code", code);

            var coupon = await _couponService.GetCouponByCodeAsync(code);

            if (coupon == null)
            {
                _logger.LogWarning("GET /api/coupon/GetByCode/{CouponCode} — Coupon not found", code);
                return NotFound(new ResponseDto<CouponDto> { IsSuccess = false, Message = "Coupon not found" });
            }

            _logger.LogInformation("GET /api/coupon/GetByCode/{CouponCode} — Coupon found (ID={CouponId})",
                code, coupon.CouponId);
            return Ok(new ResponseDto<CouponDto> { Result = coupon });
        }

        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin)]
        public async Task<IActionResult> Post(CouponDto couponDto)
        {
            _logger.LogInformation("POST /api/coupon — Creating coupon. Code={CouponCode}, DiscountAmount={DiscountAmount}",
                couponDto.CouponCode, couponDto.DiscountAmount);

            var created = await _couponService.CreateCouponAsync(couponDto);

            _logger.LogInformation("POST /api/coupon — Coupon {CouponCode} (ID={CouponId}) created successfully",
                created.CouponCode, created.CouponId);
            return Ok(new ResponseDto<CouponDto> { Result = created });
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("DELETE /api/coupon/{CouponId} — Deleting coupon", id);

            await _couponService.DeleteCouponAsync(id);

            _logger.LogInformation("DELETE /api/coupon/{CouponId} — Coupon deleted successfully", id);
            return Ok(new ResponseDto<string> { Result = "Deleted successfully" });
        }
    }
}