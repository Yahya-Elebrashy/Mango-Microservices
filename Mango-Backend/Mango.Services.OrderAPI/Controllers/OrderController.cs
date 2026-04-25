using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Services.IServices;
using Mango.Services.OrderAPI.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.OrderAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("GetOrders")]
        public async Task<IActionResult> Get(string? userId = "")
        {
            var isAdmin = User.IsInRole(SD.RoleAdmin);

            _logger.LogInformation("GET /api/order/GetOrders — RequestedBy={Scope}",
                isAdmin ? "Admin" : $"User:{userId}");

            var orders = await _orderService.GetAllOrdersAsync(isAdmin, userId!);

            _logger.LogInformation("GET /api/order/GetOrders — Returned {Count} orders", orders.Count());
            return Ok(new ResponseDto<IEnumerable<OrderHeaderDto>> { Result = orders });
        }

        [Authorize]
        [HttpGet("GetOrder/{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            _logger.LogInformation("GET /api/order/GetOrder/{OrderId} — Retrieving order", id);

            var order = await _orderService.GetOrderByIdAsync(id);

            if (order == null)
            {
                _logger.LogWarning("GET /api/order/GetOrder/{OrderId} — Order not found", id);
                return NotFound(new ResponseDto<OrderHeaderDto> { IsSuccess = false, Message = "Order not found" });
            }

            _logger.LogInformation("GET /api/order/GetOrder/{OrderId} — Order found and returned", id);
            return Ok(new ResponseDto<OrderHeaderDto> { Result = order });
        }

        [Authorize]
        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CartDto cartDto)
        {
            _logger.LogInformation("POST /api/order/CreateOrder — Creating order for user {UserId}",
                cartDto.CartHeader.UserId);

            var created = await _orderService.CreateOrderAsync(cartDto);

            _logger.LogInformation("POST /api/order/CreateOrder — Order {OrderId} created for user {UserId}",
                created.OrderHeaderId, created.UserId);

            return Ok(new ResponseDto<OrderHeaderDto> { Result = created });
        }

        [Authorize]
        [HttpPost("CreateStripeSession")]
        public async Task<IActionResult> CreateStripeSession([FromBody] StripeRequestDto stripeRequestDto)
        {
            var orderId = stripeRequestDto.OrderHeader.OrderHeaderId;

            _logger.LogInformation("POST /api/order/CreateStripeSession — Initiating Stripe session for order {OrderId}",
                orderId);

            var result = await _orderService.CreateStripeSessionAsync(stripeRequestDto);

            _logger.LogInformation(
                "POST /api/order/CreateStripeSession — Stripe session created for order {OrderId}. SessionUrl={StripeSessionUrl}",
                orderId, result.StripeSessionUrl);

            return Ok(new ResponseDto<StripeRequestDto> { Result = result });
        }

        [Authorize]
        [HttpPost("ValidateStripeSession")]
        public async Task<IActionResult> ValidateStripeSession([FromBody] int orderHeaderId)
        {
            _logger.LogInformation("POST /api/order/ValidateStripeSession — Validating Stripe session for order {OrderId}",
                orderHeaderId);

            var order = await _orderService.ValidateStripeSessionAsync(orderHeaderId);

            _logger.LogInformation(
                "POST /api/order/ValidateStripeSession — Validation complete for order {OrderId}. Status={Status}",
                orderHeaderId, order.Status);

            return Ok(new ResponseDto<OrderHeaderDto> { Result = order });
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPost("UpdateOrderStatus/{orderId:int}")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            _logger.LogInformation(
                "POST /api/order/UpdateOrderStatus/{OrderId} — Updating status to {NewStatus}",
                orderId, newStatus);

            await _orderService.UpdateOrderStatusAsync(orderId, newStatus);

            _logger.LogInformation(
                "POST /api/order/UpdateOrderStatus/{OrderId} — Status updated to {NewStatus} successfully",
                orderId, newStatus);

            return Ok(new ResponseDto<string> { Result = "Order status updated successfully" });
        }
    }
}