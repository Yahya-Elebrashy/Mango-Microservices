using AutoMapper;
using Mango.Services.CouponAPI.Constants;
using Mango.Services.CouponAPI.Data;
using Mango.Services.CouponAPI.Models;
using Mango.Services.CouponAPI.Models.Dto;
using Mango.Services.CouponAPI.Services.IServices;
using Mango.Services.CouponAPI.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        public async Task<ActionResult<ResponseDto<IEnumerable<CouponDto>>>> Get()
        {
            var response = new ResponseDto<IEnumerable<CouponDto>>();

            try
            {
                response.Result = await _couponService.GetAllCouponsAsync();

            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }

            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResponseDto<CouponDto>>> Get(int id)
        {
            var response = new ResponseDto<CouponDto>();

            try
            {
                var coupon = await _couponService.GetCouponByIdAsync(id);
                if (coupon == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Coupon not found";
                    return NotFound(response);
                }
                response.Result = coupon;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }

            return Ok(response);
        }

        [HttpGet("GetByCode/{code}")]
        public async Task<ActionResult<ResponseDto<CouponDto>>> GetByCode(string code)
        {
            var response = new ResponseDto<CouponDto>();

            try
            {
                var coupon = await _couponService.GetCouponByCodeAsync(code);
                if (coupon == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Coupon not found";
                    return NotFound(response);
                }
                response.Result = coupon;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin)]
        public async Task<ActionResult<ResponseDto<CouponDto>>> Post(CouponDto couponDto)
        {
            var response = new ResponseDto<CouponDto>();
            try
            {
                response.Result = await _couponService.CreateCouponAsync(couponDto);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ResponseDto<string>>> Delete(int id)
        {
            var response = new ResponseDto<string>();
            try
            {
                await _couponService.DeleteCouponAsync(id);
                response.Result = "Deleted successfully";
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }
    }
}
