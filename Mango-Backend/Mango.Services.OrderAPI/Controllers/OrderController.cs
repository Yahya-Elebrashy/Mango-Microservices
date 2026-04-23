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

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [Authorize]
        [HttpGet("GetOrders")]
        public async Task<ActionResult<ResponseDto<IEnumerable<OrderHeaderDto>>>> Get(string? userId = "")
        {
            return Ok(new ResponseDto<IEnumerable<OrderHeaderDto>>
            {
                Result = await _orderService.GetAllOrdersAsync(User.IsInRole(SD.RoleAdmin), userId!)
            });
        }

        [Authorize]
        [HttpGet("GetOrder/{id:int}")]
        public async Task<ActionResult<ResponseDto<OrderHeaderDto>>> Get(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);

            if (order == null)
                return NotFound(new ResponseDto<OrderHeaderDto> { IsSuccess = false, Message = "Order not found" });

            return Ok(new ResponseDto<OrderHeaderDto> { Result = order });
        }

        [Authorize]
        [HttpPost("CreateOrder")]
        public async Task<ActionResult<ResponseDto<OrderHeaderDto>>> CreateOrder([FromBody] CartDto cartDto)
        {
            return Ok(new ResponseDto<OrderHeaderDto>
            {
                Result = await _orderService.CreateOrderAsync(cartDto)
            });
        }

        [Authorize]
        [HttpPost("CreateStripeSession")]
        public async Task<ActionResult<ResponseDto<StripeRequestDto>>> CreateStripeSession([FromBody] StripeRequestDto stripeRequestDto)
        {
            return Ok(new ResponseDto<StripeRequestDto>
            {
                Result = await _orderService.CreateStripeSessionAsync(stripeRequestDto)
            });
        }

        [Authorize]
        [HttpPost("ValidateStripeSession")]
        public async Task<ActionResult<ResponseDto<OrderHeaderDto>>> ValidateStripeSession([FromBody] int orderHeaderId)
        {
            return Ok(new ResponseDto<OrderHeaderDto>
            {
                Result = await _orderService.ValidateStripeSessionAsync(orderHeaderId)
            });
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPost("UpdateOrderStatus/{orderId:int}")]
        public async Task<ActionResult<ResponseDto<string>>> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            await _orderService.UpdateOrderStatusAsync(orderId, newStatus);
            return Ok(new ResponseDto<string> { Result = "Order status updated successfully" });
        }
    }
}