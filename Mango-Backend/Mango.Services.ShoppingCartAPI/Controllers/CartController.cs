using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("GetCart/{userId}")]
        public async Task<ActionResult<ResponseDto<CartDto>>> GetCart(string userId)
        {
            return Ok(new ResponseDto<CartDto>
            {
                Result = await _cartService.GetCartAsync(userId)
            });
        }

        [HttpPost("CartUpsert")]
        public async Task<ActionResult<ResponseDto<CartDto>>> CartUpsert(CartDto cartDto)
        {
            return Ok(new ResponseDto<CartDto>
            {
                Result = await _cartService.UpsertCartAsync(cartDto)
            });
        }

        [HttpDelete("RemoveCart/{cartDetailsId}")]
        public async Task<ActionResult<ResponseDto<bool>>> RemoveCart(int cartDetailsId)
        {
            await _cartService.RemoveCartItemAsync(cartDetailsId);
            return Ok(new ResponseDto<bool> { Result = true });
        }

        [HttpPost("ApplyCoupon")]
        public async Task<ActionResult<ResponseDto<bool>>> ApplyCoupon([FromBody] CartDto cartDto)
        {
            if (cartDto?.CartHeader == null || string.IsNullOrEmpty(cartDto.CartHeader.UserId))
                return BadRequest(new ResponseDto<bool> { IsSuccess = false, Message = "Invalid request" });

            await _cartService.ApplyCouponAsync(cartDto.CartHeader.UserId, cartDto.CartHeader.CouponCode);
            return Ok(new ResponseDto<bool> { Result = true, Message = "Coupon applied successfully" });
        }

        [HttpPost("RemoveCoupon")]
        public async Task<ActionResult<ResponseDto<bool>>> RemoveCoupon([FromBody] CartDto cartDto)
        {
            if (cartDto?.CartHeader == null || string.IsNullOrEmpty(cartDto.CartHeader.UserId))
                return BadRequest(new ResponseDto<bool> { IsSuccess = false, Message = "Invalid request" });

            await _cartService.RemoveCouponAsync(cartDto.CartHeader.UserId);
            return Ok(new ResponseDto<bool> { Result = true, Message = "Coupon removed successfully" });
        }

        [HttpPost("EmailCartRequest")]
        public async Task<ActionResult<ResponseDto<bool>>> EmailCartRequest([FromBody] CartDto cartDto)
        {
            await _cartService.EmailCartRequestAsync(cartDto);
            return Ok(new ResponseDto<bool> { Result = true, Message = "Email request sent to queue successfully" });
        }
    }
}