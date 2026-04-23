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

        public CouponController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(new ResponseDto<IEnumerable<CouponDto>>
            {
                Result = await _couponService.GetAllCouponsAsync()
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var coupon = await _couponService.GetCouponByIdAsync(id);
            if (coupon == null)
                return NotFound(new ResponseDto<CouponDto> { IsSuccess = false, Message = "Coupon not found" });
            return Ok(new ResponseDto<CouponDto> { Result = coupon });
        }

        [HttpGet("GetByCode/{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var coupon = await _couponService.GetCouponByCodeAsync(code);
            if (coupon == null)
                return NotFound(new ResponseDto<CouponDto> { IsSuccess = false, Message = "Coupon not found" });
            return Ok(new ResponseDto<CouponDto> { Result = coupon });
        }

        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin)]
        public async Task<IActionResult> Post(CouponDto couponDto)
        {
            return Ok(new ResponseDto<CouponDto>
            {
                Result = await _couponService.CreateCouponAsync(couponDto)
            });
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _couponService.DeleteCouponAsync(id);
            return Ok(new ResponseDto<string> { Result = "Deleted successfully" });
        }
    }
}