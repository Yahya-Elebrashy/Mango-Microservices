using AutoMapper;
using Azure;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service.IService;
using MessageBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Reflection.PortableExecutable;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly IProductService _productService;
        private readonly ICouponService _couponService;
        private readonly IMessageBus _messageBus;
        private readonly IConfiguration _configuration;
        public CartController(AppDbContext db, IMapper mapper, IProductService productService, ICouponService couponService, IMessageBus messageBus, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _productService = productService;
            _messageBus = messageBus;
            _configuration = configuration;
            _couponService = couponService;
        }

        [HttpPost("CartUpsert")]
        public async Task<ActionResult<ResponseDto<CartDto>>> CartUpsert(CartDto CartDto)
        {
            var response = new ResponseDto<CartDto>();
            try
            {
                CartHeader cartHeaderFromDb = await _db.CartHeaders.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == CartDto.CartHeader.UserId);
                if (cartHeaderFromDb == null)
                {
                    CartHeader cartHeader = _mapper.Map<CartHeader>(CartDto.CartHeader);
                    _db.CartHeaders.Add(cartHeader);
                    await _db.SaveChangesAsync();
                    CartDto.CartDetails.First().CartHeaderId = cartHeader.CartHeaderId;
                    _db.CartDetails.Add(_mapper.Map<CartDetails>(CartDto.CartDetails.First()));
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // if cartHeader not null
                    // check if details has same product
                    var cartDetailsFromDb = await _db.CartDetails.AsNoTracking().FirstOrDefaultAsync(
                        p => p.ProductId == CartDto.CartDetails.First().ProductId
                        && p.CartHeaderId == cartHeaderFromDb.CartHeaderId
                        );
                    if (cartDetailsFromDb == null)
                    {
                        // create cartDetails
                        CartDto.CartDetails.First().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                        _db.CartDetails.Add(_mapper.Map<CartDetails>(CartDto.CartDetails.First()));
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        // update count of catr details
                        CartDto.CartDetails.First().Count += cartDetailsFromDb.Count;
                        CartDto.CartDetails.First().CartHeaderId = cartDetailsFromDb.CartHeaderId;
                        CartDto.CartDetails.First().CartDetailsId = cartDetailsFromDb.CartDetailsId;
                        _db.CartDetails.Update(_mapper.Map<CartDetails>(CartDto.CartDetails.First()));
                        await _db.SaveChangesAsync();
                    }
                }
                response.Result = CartDto;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }

            return Ok(response);
        }
        [HttpDelete("RemoveCart/{cartDetailsId}")]
        public async Task<ActionResult<ResponseDto<bool>>> RemoveCart(int cartDetailsId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                CartDetails cartDetails = await _db.CartDetails.FirstOrDefaultAsync(
                    c => c.CartDetailsId == cartDetailsId);
                if (cartDetails == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Item not found";
                    return NotFound(response);
                }
                int totalCountOfCartItem = await _db.CartDetails.CountAsync(x => x.CartHeaderId == cartDetails.CartHeaderId);
                _db.CartDetails.Remove(cartDetails);
                if (totalCountOfCartItem == 1)
                {
                    var header = await _db.CartHeaders.FirstOrDefaultAsync(c => c.CartHeaderId == cartDetails.CartHeaderId);
                    if (header != null)
                        _db.CartHeaders.Remove(header);
                }
                await _db.SaveChangesAsync();
                response.Result = true;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }

            return Ok(response);
        }
        [HttpGet("GetCart/{userId}")]
        public async Task<ActionResult<ResponseDto<CartDto>>> GetCart(string userId)
        {
            var response = new ResponseDto<CartDto>();
            try
            {
                var header = await _db.CartHeaders.FirstOrDefaultAsync(c => c.UserId == userId);
                if (header == null)
                {
                    response.Result = new CartDto();
                    return Ok(response);
                }
                CartDto cartDto = new CartDto
                {
                    CartHeader = _mapper.Map<CartHeaderDto>(header)
                };
                cartDto.CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(_db.CartDetails.Where(c => c.CartHeaderId == cartDto.CartHeader.CartHeaderId));
                var products = await _productService.GetProductsAsync();
                foreach (var item in cartDto.CartDetails)
                {
                    item.ProductDto = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                    cartDto.CartHeader.CartTotal += (item.Count * item.ProductDto.Price);
                }
                if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
                {
                    var coupon = await _couponService.GetCouponAsync(cartDto.CartHeader.CouponCode);
                    if (coupon != null && cartDto.CartHeader.CartTotal > coupon.MinAmount)
                    {
                        cartDto.CartHeader.CartTotal -= coupon.DiscountAmount;
                        cartDto.CartHeader.Discount = coupon.DiscountAmount;
                    }
                }
                response.Result = cartDto;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }

            return Ok(response);
        }
        [HttpPost("ApplyCoupon")]
        public async Task<ActionResult<ResponseDto<bool>>> ApplyCoupon([FromBody] CartDto cartDto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                if (cartDto?.CartHeader == null || string.IsNullOrEmpty(cartDto.CartHeader.UserId))
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid request";
                    return BadRequest(response);
                }
                var cartFromDb = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == cartDto.CartHeader.UserId);
                if (cartFromDb == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Cart not found";
                    return NotFound(response);
                }
                var coupon = await _couponService.GetCouponAsync(cartDto.CartHeader.CouponCode);

                if (coupon == null || string.IsNullOrEmpty(coupon.CouponCode))
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid coupon code";
                    return BadRequest(response);
                }
                cartFromDb.CouponCode = cartDto.CartHeader.CouponCode;
                _db.CartHeaders.Update(cartFromDb);
                await _db.SaveChangesAsync();
                response.Result = true;
                response.Message = "Coupon applied successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }

            return Ok(response);
        }
        [HttpPost("RemoveCoupon")]
        public async Task<ActionResult<ResponseDto<bool>>> RemoveCoupon([FromBody] CartDto cartDto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                if (cartDto?.CartHeader == null || string.IsNullOrEmpty(cartDto.CartHeader.UserId))
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid request";
                    return BadRequest(response);
                }
                var cartFromDb = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == cartDto.CartHeader.UserId);
                if (cartFromDb == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Cart not found";
                    return NotFound(response);
                }
                cartFromDb.CouponCode = "";
                await _db.SaveChangesAsync();
                response.Result = true;
                response.Message = "Coupon removed successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }
        [HttpPost("EmailCartRequest")]
        public async Task<ActionResult<ResponseDto<bool>>> EmailCartRequest([FromBody] CartDto cartDto)
        {
            var response = new ResponseDto<bool>();
            try
            {
                // Publish message to RabbitMQ
                var queueName = _configuration.GetValue<string>("RabbitMQ:EmailQueue") ?? "emailcartqueue";
                await _messageBus.PublishMessage(cartDto, queueName);

                response.Result = true;
                response.Message = "Email request sent to queue successfully";
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
