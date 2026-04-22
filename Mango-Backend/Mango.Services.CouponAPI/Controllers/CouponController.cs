using AutoMapper;
using Mango.Services.CouponAPI.Constants;
using Mango.Services.CouponAPI.Data;
using Mango.Services.CouponAPI.Models;
using Mango.Services.CouponAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.CouponAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public CouponController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }
        [Authorize]
        [HttpGet]
        public ActionResult<ResponseDto<IEnumerable<CouponDto>>> Get()
        {
            var response = new ResponseDto<IEnumerable<CouponDto>>();

            try
            {
                var coupons = _db.Coupons.ToList();
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
        public ActionResult<ResponseDto<Coupon>> Get(int id)
        {
            var response = new ResponseDto<Coupon>();

            try
            {
                var coupon = _db.Coupons.FirstOrDefault(x => x.CouponId == id);

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
        public ActionResult<ResponseDto<CouponDto>> GetByCode(string code)
        {
            var response = new ResponseDto<CouponDto>();

            try
            {
                var coupon = _db.Coupons
                    .FirstOrDefault(x => x.CouponCode.ToLower() == code.ToLower());

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
        public ActionResult<ResponseDto<CouponDto>> Post(CouponDto couponDto)
        {
            var response = new ResponseDto<CouponDto>();

            try
            {
                var coupon = _mapper.Map<Coupon>(couponDto);

                _db.Coupons.Add(coupon);
                _db.SaveChanges();

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
        public ActionResult<ResponseDto<string>> Delete(int id)
        {
            var response = new ResponseDto<string>();

            try
            {
                var coupon = _db.Coupons.FirstOrDefault(x => x.CouponId == id);

                if (coupon == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Coupon not found";
                    return NotFound(response);
                }

                _db.Coupons.Remove(coupon);
                _db.SaveChanges();

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
