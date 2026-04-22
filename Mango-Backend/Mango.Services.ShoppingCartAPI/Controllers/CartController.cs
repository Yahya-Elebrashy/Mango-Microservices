using AutoMapper;
using Azure;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Mango.Services.ShoppingCartAPI.UnitOfWork;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IProductService _productService;
        private readonly ICouponService _couponService;
        private readonly IMessageBus _messageBus;
        private readonly IConfiguration _configuration;
        public CartController(IUnitOfWork unitOfWork, IMapper mapper, IProductService productService, ICouponService couponService, IMessageBus messageBus, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _productService = productService;
            _messageBus = messageBus;
            _configuration = configuration;
            _couponService = couponService;
        }

        [HttpPost("CartUpsert")]
        public async Task<ActionResult<ResponseDto<CartDto>>> CartUpsert(CartDto cartDto)
        {
            var response = new ResponseDto<CartDto>();
            try
            {
                CartHeader cartHeaderFromDb = await _unitOfWork.CartHeader
                                                               .GetAsync(c => c.UserId == cartDto.CartHeader.UserId);
                if (cartHeaderFromDb == null)
                {
                    CartHeader newHeader = _mapper.Map<CartHeader>(cartDto.CartHeader);
                    await _unitOfWork.CartHeader.CreateAsync(newHeader);
                    await _unitOfWork.SaveAsync();

                    cartDto.CartDetails.First().CartHeaderId = newHeader.CartHeaderId;
                    await _unitOfWork.CartDetails.CreateAsync(
                        _mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                    await _unitOfWork.SaveAsync();
                }
                else
                {
                    // if cartHeader not null
                    // check if details has same product
                    var cartDetailsFromDb = await _unitOfWork.CartDetails.GetAsync(
                                                  c => c.ProductId == cartDto.CartDetails.First().ProductId
                                                  && c.CartHeaderId == cartHeaderFromDb.CartHeaderId);
                    if (cartDetailsFromDb == null)
                    {
                        // create cartDetails
                        cartDto.CartDetails.First().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                        await _unitOfWork.CartDetails.CreateAsync(
                                          _mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                    }
                    else
                    {
                        // update count of catr details
                        cartDto.CartDetails.First().Count += cartDetailsFromDb.Count;
                        cartDto.CartDetails.First().CartHeaderId = cartDetailsFromDb.CartHeaderId;
                        cartDto.CartDetails.First().CartDetailsId = cartDetailsFromDb.CartDetailsId;
                        await _unitOfWork.CartDetails.UpdateAsync(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                    }
                    await _unitOfWork.SaveAsync();
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
        [HttpDelete("RemoveCart/{cartDetailsId}")]
        public async Task<ActionResult<ResponseDto<bool>>> RemoveCart(int cartDetailsId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                CartDetails cartDetails = await _unitOfWork.CartDetails
                                          .GetAsync(c => c.CartDetailsId == cartDetailsId);

                if (cartDetails == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Item not found";
                    return NotFound(response);
                }
                int remainingItems = await _unitOfWork.CartDetails
                                           .CountByHeaderIdAsync(cartDetails.CartHeaderId);

                await _unitOfWork.CartDetails.RemoveAsync(cartDetails);
                if (remainingItems == 1)
                {
                    var header = await _unitOfWork.CartHeader
                                 .GetAsync(c => c.CartHeaderId == cartDetails.CartHeaderId);
 
                    if (header != null)
                        await _unitOfWork.CartHeader.RemoveAsync(header);
                }
                await _unitOfWork.SaveAsync();

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
                var cartHeader = await _unitOfWork.CartHeader.GetAsync(c => c.UserId == userId);
                if (cartHeader is null)
                    return new ResponseDto<CartDto> { IsSuccess = false, Message = "Cart not found" };

                var cartDetails = await _unitOfWork.CartDetails
                    .GetAllAsync()
                    .ContinueWith(t => t.Result.Where(c => c.CartHeaderId == cartHeader.CartHeaderId));

                var cartDto = new CartDto
                {
                    CartHeader = _mapper.Map<CartHeaderDto>(cartHeader),
                    CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(cartDetails)
                };

                // Enrich with product info
                var products = await _productService.GetProductsAsync();
                foreach (var item in cartDto.CartDetails)
                {
                    item.ProductDto = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                    cartDto.CartHeader.CartTotal += item.Count * item.ProductDto?.Price ?? 0;
                }

                // Apply coupon
                if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
                {
                    var coupon = await _couponService.GetCouponAsync(cartDto.CartHeader.CouponCode);
                    if (coupon != null && cartDto.CartHeader.CartTotal >= coupon.MinAmount)
                    {
                        cartDto.CartHeader.CartTotal -= coupon.DiscountAmount;
                        cartDto.CartHeader.Discount = coupon.DiscountAmount;
                    }
                }

                cartDto.CartHeader.CartTotal = Math.Round(cartDto.CartHeader.CartTotal, 2);
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
                var cartFromDb = await _unitOfWork.CartHeader
                                .GetAsync(c => c.UserId == cartDto.CartHeader.UserId);
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
                await _unitOfWork.CartHeader.UpdateAsync(cartFromDb);
                await _unitOfWork.SaveAsync();

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
                var cartFromDb = await _unitOfWork.CartHeader.GetAsync(u => u.UserId == cartDto.CartHeader.UserId);
                if (cartFromDb == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Cart not found";
                    return NotFound(response);
                }
                cartFromDb.CouponCode = "";
                await _unitOfWork.SaveAsync();
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
