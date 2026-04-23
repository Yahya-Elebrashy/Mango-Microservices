using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Services;
using Mango.Services.OrderAPI.Services.IServices;
using Mango.Services.OrderAPI.UnitOfWork;
using Mango.Services.OrderAPI.Utility;
using MessageBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
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
            var response = new ResponseDto<IEnumerable<OrderHeaderDto>>();
            try
            {
                response.Result = await _orderService.GetAllOrdersAsync(User.IsInRole(SD.RoleAdmin), userId!);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }

        [Authorize]
        [HttpGet("GetOrder/{id:int}")]
        public async Task<ActionResult<ResponseDto<OrderHeaderDto>>> Get(int id)
        {
            var response = new ResponseDto<OrderHeaderDto>();
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Order not found";
                    return NotFound(response);
                }
                response.Result = order;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }

        [Authorize]
        [HttpPost("CreateOrder")]
        public async Task<ActionResult<ResponseDto<OrderHeaderDto>>> CreateOrder([FromBody] CartDto cartDto)
        {
            var response = new ResponseDto<OrderHeaderDto>();
            try
            {
                response.Result = await _orderService.CreateOrderAsync(cartDto);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }
        [Authorize]
        [HttpPost("CreateStripeSession")]
        public async Task<ActionResult<ResponseDto<StripeRequestDto>>> CreateStripeSession([FromBody] StripeRequestDto stripeRequestDto)
        {
            var response = new ResponseDto<StripeRequestDto>();
            try
            {
                response.Result = await _orderService.CreateStripeSessionAsync(stripeRequestDto);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return StatusCode(500, response);
            }
            return Ok(response);
        }
        [Authorize]
        [HttpPost("ValidateStripeSession")]
        public async Task<ActionResult<ResponseDto<OrderHeaderDto>>> ValidateStripeSession([FromBody] int orderHeaderId)
        {
            var response = new ResponseDto<OrderHeaderDto>();
            try
            {
                response.Result = await _orderService.ValidateStripeSessionAsync(orderHeaderId);
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

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPost("UpdateOrderStatus/{orderId:int}")]
        public async Task<ActionResult<ResponseDto<string>>> UpdateOrderStatus(int orderId, [FromBody] string newStatus)
        {
            var response = new ResponseDto<string>();
            try
            {
                await _orderService.UpdateOrderStatusAsync(orderId, newStatus);
                response.Result = "Order status updated successfully";
            }
            catch (KeyNotFoundException ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return NotFound(response);
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                return BadRequest(response);
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
