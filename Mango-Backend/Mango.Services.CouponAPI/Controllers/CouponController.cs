using AutoMapper;
using Mango.Services.CouponAPI.Constants;
using Mango.Services.CouponAPI.Data;
using Mango.Services.CouponAPI.Models;
using Mango.Services.CouponAPI.Models.Dto;
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
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public CouponController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<ResponseDto<IEnumerable<CouponDto>>>> Get()
        {
            var response = new ResponseDto<IEnumerable<CouponDto>>();

            try
            {
                var coupons = await _unitOfWork.Coupon.GetAllAsync();
                response.Result = _mapper.Map<IEnumerable<CouponDto>>(coupons);
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
        public async Task<ActionResult<ResponseDto<Coupon>>> Get(int id)
        {
            var response = new ResponseDto<Coupon>();

            try
            {
                var coupon = await _unitOfWork.Coupon.GetAsync(c => c.CouponId == id);

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
                var coupon = await _unitOfWork.Coupon.GetByCodeAsync(code);

                if (coupon == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Coupon not found";
                    return NotFound(response);
                }

                response.Result = _mapper.Map<CouponDto>(coupon);
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
                var coupon = _mapper.Map<Coupon>(couponDto);

                await _unitOfWork.Coupon.CreateAsync(coupon);
                await _unitOfWork.SaveAsync();

                // Stripe
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
                catch (Exception stripeEx)
                {
                    // log بس — متكسرش العملية
                    response.Message = $"Created locally but Stripe failed: {stripeEx.Message}";
                }

                response.Result = _mapper.Map<CouponDto>(coupon);
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
                var coupon = await _unitOfWork.Coupon.GetAsync(c => c.CouponId == id);

                if (coupon == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Coupon not found";
                    return NotFound(response);
                }

                await _unitOfWork.Coupon.RemoveAsync(coupon);
                await _unitOfWork.SaveAsync();

                // Stripe
                try
                {
                    var service = new Stripe.CouponService();
                    service.Delete(coupon.CouponCode);
                }
                catch (Exception stripeEx)
                {
                    response.Message = $"Deleted locally but Stripe failed: {stripeEx.Message}";
                }

                response.Result = "Deleted successfully";
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
