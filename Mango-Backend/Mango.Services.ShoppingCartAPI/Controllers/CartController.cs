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
        private ResponseDto _responseDto;
        public CartController(AppDbContext db, IMapper mapper, IProductService productService, ICouponService couponService, IMessageBus messageBus, IConfiguration configuration)
        {
            _db = db;
            _responseDto = new();
            _mapper = mapper;
            _productService = productService;
            _messageBus = messageBus;
            _configuration = configuration;
            _couponService = couponService;
        }

        [HttpPost("CartUpsert")]
        public async Task<ResponseDto> CartUpsert(CartDto CartDto)
        {
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
                _responseDto.Result = CartDto;
            }
            catch (Exception ex)
            {

                _responseDto.Message = ex.Message.ToString();
                _responseDto.IsSuccess = false;
            }
            return _responseDto;
        }
        [HttpDelete("RemoveCart/{cartDetailsId}")]
        public async Task<ResponseDto> RemoveCart(int cartDetailsId)
        {
            try
            {
                CartDetails cartDetails = _db.CartDetails.First(
                    c => c.CartDetailsId == cartDetailsId);
                int totalCountOfCartItem = _db.CartDetails.Where(
                    c => c.CartHeaderId == cartDetails.CartHeaderId).Count();
                _db.CartDetails.Remove(cartDetails);
                if (totalCountOfCartItem == 1)
                {
                    var cartHeaderToRemove = _db.CartHeaders.FirstOrDefault(c => c.CartHeaderId == cartDetails.CartHeaderId);
                    _db.CartHeaders.Remove(cartHeaderToRemove);
                }
                await _db.SaveChangesAsync();
                _responseDto.Result = true;
            }
            catch (Exception ex)
            {

                _responseDto.Message = ex.Message.ToString();
                _responseDto.IsSuccess = false;
            }
            return _responseDto;
        }
        [HttpGet("GetCart/{userId}")]
        public async Task<ResponseDto> GetCart(string userId)
        
        {
            try
            {
                var cartHeaderFromDb = _db.CartHeaders.FirstOrDefault(c => c.UserId == userId);
                if (cartHeaderFromDb == null)
                {
                    _responseDto.Result = new CartDto();
                    return _responseDto;
                }
                CartDto cartDto = new CartDto
                {
                    CartHeader = _mapper.Map<CartHeaderDto>(cartHeaderFromDb)
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
                _responseDto.Result = cartDto;
            }
            catch (Exception ex)
            {

                _responseDto.Message = ex.Message.ToString();
                _responseDto.IsSuccess = false;
            }
            return _responseDto;
        }
        [HttpPost("ApplyCoupon")]
        public async Task<ResponseDto> ApplyCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var cartFromDb = await _db.CartHeaders.FirstAsync(u => u.UserId == cartDto.CartHeader.UserId);
                cartFromDb.CouponCode = cartDto.CartHeader.CouponCode;
                _db.CartHeaders.Update(cartFromDb);
                await _db.SaveChangesAsync();
                _responseDto.Result = true;
            }
            catch (Exception ex)
            {

                _responseDto.Message = ex.Message.ToString();
                _responseDto.IsSuccess = false;
            }
            return _responseDto;
        }
        [HttpPost("RemoveCoupon")]
        public async Task<ResponseDto> RemoveCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var cartFromDb = await _db.CartHeaders.FirstAsync(u => u.UserId == cartDto.CartHeader.UserId);
                cartFromDb.CouponCode = "";
                _db.CartHeaders.Update(cartFromDb);
                await _db.SaveChangesAsync();
                _responseDto.Result = true;
            }
            catch (Exception ex)
            {

                _responseDto.Message = ex.Message.ToString();
                _responseDto.IsSuccess = false;
            }
            return _responseDto;
        }
        [HttpPost("EmailCartRequest")]
        public async Task<ResponseDto> EmailCartRequest([FromBody] CartDto cartDto)
        {
            try
            {
                // Publish message to RabbitMQ
                var queueName = _configuration.GetValue<string>("RabbitMQ:EmailQueue") ?? "emailcartqueue";
                await _messageBus.PublishMessage(cartDto, queueName);

                _responseDto.Result = true;
                _responseDto.Message = "Email request sent to queue successfully";
            }
            catch (Exception ex)
            {
                _responseDto.IsSuccess = false;
                _responseDto.Message = ex.Message;
            }
            return _responseDto;
        }

    }
}
