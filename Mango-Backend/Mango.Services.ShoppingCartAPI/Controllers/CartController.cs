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
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        [HttpGet("GetCart/{userId}")]
        public async Task<IActionResult> GetCart(string userId)
        {
            _logger.LogInformation("GET /api/cart/GetCart — Fetching cart for user {UserId}", userId);

            var cart = await _cartService.GetCartAsync(userId);

            _logger.LogInformation("GET /api/cart/GetCart — Cart returned for user {UserId}. ItemCount={ItemCount}, Total={CartTotal}",
                userId, cart.CartDetails?.Count(), cart.CartHeader.CartTotal);

            return Ok(new ResponseDto<CartDto> { Result = cart });
        }

        [HttpPost("CartUpsert")]
        public async Task<IActionResult> CartUpsert(CartDto cartDto)
        {
            var userId = cartDto.CartHeader.UserId;
            var productId = cartDto.CartDetails?.First().ProductId;

            _logger.LogInformation("POST /api/cart/CartUpsert — Upserting cart for user {UserId}. ProductId={ProductId}",
                userId, productId);

            var result = await _cartService.UpsertCartAsync(cartDto);

            _logger.LogInformation("POST /api/cart/CartUpsert — Cart upsert complete for user {UserId}. ProductId={ProductId}",
                userId, productId);

            return Ok(new ResponseDto<CartDto> { Result = result });
        }

        [HttpDelete("RemoveCart/{cartDetailsId}")]
        public async Task<IActionResult> RemoveCart(int cartDetailsId)
        {
            _logger.LogInformation("DELETE /api/cart/RemoveCart/{CartDetailsId} — Removing cart item", cartDetailsId);

            await _cartService.RemoveCartItemAsync(cartDetailsId);

            _logger.LogInformation("DELETE /api/cart/RemoveCart/{CartDetailsId} — Item removed successfully", cartDetailsId);

            return Ok(new ResponseDto<string> { Result = "Item removed successfully" });
        }

        [HttpPost("ApplyCoupon")]
        public async Task<IActionResult> ApplyCoupon([FromBody] CartDto cartDto)
        {
            var userId = cartDto?.CartHeader?.UserId;
            var couponCode = cartDto?.CartHeader?.CouponCode;

            if (cartDto?.CartHeader == null || string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("POST /api/cart/ApplyCoupon — Invalid request: missing UserId or CartHeader");
                return BadRequest(new ResponseDto<bool> { IsSuccess = false, Message = "Invalid request" });
            }

            _logger.LogInformation("POST /api/cart/ApplyCoupon — Applying coupon {CouponCode} for user {UserId}",
                couponCode, userId);

            await _cartService.ApplyCouponAsync(userId, couponCode!);

            _logger.LogInformation("POST /api/cart/ApplyCoupon — Coupon {CouponCode} applied successfully for user {UserId}",
                couponCode, userId);

            return Ok(new ResponseDto<bool> { Result = true, Message = "Coupon applied successfully" });
        }

        [HttpPost("RemoveCoupon")]
        public async Task<IActionResult> RemoveCoupon([FromBody] CartDto cartDto)
        {
            var userId = cartDto?.CartHeader?.UserId;

            if (cartDto?.CartHeader == null || string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("POST /api/cart/RemoveCoupon — Invalid request: missing UserId or CartHeader");
                return BadRequest(new ResponseDto<bool> { IsSuccess = false, Message = "Invalid request" });
            }

            _logger.LogInformation("POST /api/cart/RemoveCoupon — Removing coupon for user {UserId}", userId);

            await _cartService.RemoveCouponAsync(userId);

            _logger.LogInformation("POST /api/cart/RemoveCoupon — Coupon removed successfully for user {UserId}", userId);

            return Ok(new ResponseDto<bool> { Result = true, Message = "Coupon removed successfully" });
        }

        [HttpPost("EmailCartRequest")]
        public async Task<IActionResult> EmailCartRequest([FromBody] CartDto cartDto)
        {
            var userId = cartDto.CartHeader.UserId;

            _logger.LogInformation("POST /api/cart/EmailCartRequest — Publishing email request for user {UserId}", userId);

            await _cartService.EmailCartRequestAsync(cartDto);

            _logger.LogInformation("POST /api/cart/EmailCartRequest — Email request queued for user {UserId}", userId);

            return Ok(new ResponseDto<bool> { Result = true, Message = "Email request sent to queue successfully" });
        }
    }
}