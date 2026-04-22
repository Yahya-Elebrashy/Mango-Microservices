using AutoMapper;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
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
        private IMapper _mapper;
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IMessageBus _messageBus;

        public OrderController(AppDbContext db, IMapper mapper, IConfiguration configuration, IMessageBus messageBus)
        {
            _db = db;
            _mapper = mapper;
            _messageBus = messageBus;
            _configuration = configuration;
        }
        [Authorize]
        [HttpGet("GetOrders")]
        public async Task<ActionResult<ResponseDto<IEnumerable<OrderHeaderDto>>>> Get(string? userId = "")
        {
            var response = new ResponseDto<IEnumerable<OrderHeaderDto>>();
            try
            {
                IEnumerable<OrderHeader> orders;
                if (User.IsInRole(SD.RoleAdmin))
                {
                    orders = _db.OrderHeaders.Include(u => u.OrderDetails).OrderByDescending(u => u.OrderHeaderId).ToList();
                }
                else
                {
                    orders = _db.OrderHeaders.Include(u => u.OrderDetails).Where(u => u.UserId == userId).OrderByDescending(u => u.OrderHeaderId).ToList();
                }
                response.Result = _mapper.Map<IEnumerable<OrderHeaderDto>>(orders);
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
                OrderHeader order = _db.OrderHeaders.Include(u => u.OrderDetails).First(u => u.OrderHeaderId == id);
                if (order == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Order not found";
                    return NotFound(response);
                }
                response.Result = _mapper.Map<OrderHeaderDto>(order);
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
                OrderHeaderDto orderHeaderDto = _mapper.Map<OrderHeaderDto>(cartDto.CartHeader);
                orderHeaderDto.OrderTime = DateTime.Now;
                orderHeaderDto.Status = SD.Status_Pending;
                orderHeaderDto.OrderDetails = _mapper.Map<IEnumerable<OrderDetailsDto>>(cartDto.CartDetails);
                orderHeaderDto.OrderTotal = Math.Round(orderHeaderDto.OrderTotal, 2);
                OrderHeader orderCreated = _db.OrderHeaders.Add(_mapper.Map<OrderHeader>(orderHeaderDto)).Entity;
                await _db.SaveChangesAsync();

                orderHeaderDto.OrderHeaderId = orderCreated.OrderHeaderId;
                response.Result = orderHeaderDto;
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

                var options = new SessionCreateOptions
                {
                    SuccessUrl = stripeRequestDto.ApprovedUrl,
                    CancelUrl = stripeRequestDto.CancelUrl,
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };
                var DiscountsObj = new List<SessionDiscountOptions>()
                {
                    new SessionDiscountOptions
                    {
                        Coupon=stripeRequestDto.OrderHeader.CouponCode
                    }
                };
                foreach (var item in stripeRequestDto.OrderHeader.OrderDetails)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100), 
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.ProductName
                            }
                        },
                        Quantity = item.Count
                    };

                    options.LineItems.Add(sessionLineItem);
                }
                if (stripeRequestDto.OrderHeader.Discount > 0)
                {
                    options.Discounts = DiscountsObj;
                }

                var service = new SessionService();
                Session session = service.Create(options);
                stripeRequestDto.StripeSessionUrl = session.Url;
                OrderHeader orderHeader = _db.OrderHeaders.First(u => u.OrderHeaderId == stripeRequestDto.OrderHeader.OrderHeaderId);
                orderHeader.StripeSessionId = session.Id;
                _db.SaveChanges();
                response.Result = stripeRequestDto;

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

                OrderHeader orderHeader = await _db.OrderHeaders.FirstOrDefaultAsync(u => u.OrderHeaderId == orderHeaderId);
                if (orderHeader == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Order not found";
                    return NotFound(response);
                }
                var service = new SessionService();
                Session session = service.Get(orderHeader.StripeSessionId);

                var paymentIntentService = new PaymentIntentService();
                PaymentIntent paymentIntent = paymentIntentService.Get(session.PaymentIntentId);

                if (paymentIntent.Status == "succeeded")
                {
                    //then payment was successful
                    orderHeader.PaymentIntentId = paymentIntent.Id;
                    orderHeader.Status = SD.Status_Approved;
                    _db.SaveChanges();
                    RewardsDto rewardsDto = new()
                    {
                        OrderId = orderHeader.OrderHeaderId,
                        RewardsActivity = Convert.ToInt32(orderHeader.OrderTotal),
                        UserId = orderHeader.UserId
                    };
                    string topicName = _configuration.GetValue<string>("RabbitMQ:OrderCreatedQueue") ?? "OrderCreatedQueue"; 
                    await _messageBus.PublishMessage(rewardsDto, topicName);
                    response.Result = _mapper.Map<OrderHeaderDto>(orderHeader);
                }

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
                var order = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.OrderHeaderId == orderId);

                if (order == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Order not found";
                    return NotFound(response);
                }

                // Refund لو cancelled
                if (newStatus == SD.Status_Cancelled)
                {
                    if (string.IsNullOrEmpty(order.PaymentIntentId))
                    {
                        response.IsSuccess = false;
                        response.Message = "No payment found to refund";
                        return BadRequest(response);
                    }

                    try
                    {
                        var refundService = new RefundService();

                        var refund = refundService.Create(new RefundCreateOptions
                        {
                            Reason = RefundReasons.RequestedByCustomer,
                            PaymentIntent = order.PaymentIntentId
                        });
                    }
                    catch (Exception stripeEx)
                    {
                        response.IsSuccess = false;
                        response.Message = $"Refund failed: {stripeEx.Message}";
                        return StatusCode(500, response);
                    }
                }

                order.Status = newStatus;
                await _db.SaveChangesAsync();

                response.Result = "Order status updated successfully";
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
