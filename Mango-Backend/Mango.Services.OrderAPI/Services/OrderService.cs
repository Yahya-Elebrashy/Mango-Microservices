using AutoMapper;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Services.IServices;
using Mango.Services.OrderAPI.UnitOfWork;
using Mango.Services.OrderAPI.Utility;
using MessageBus;
using Stripe;
using Stripe.Checkout;

namespace Mango.Services.OrderAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageBus _messageBus;
        private readonly IConfiguration _configuration;

        public OrderService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageBus messageBus,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageBus = messageBus;
            _configuration = configuration;
        }

        public async Task<IEnumerable<OrderHeaderDto>> GetAllOrdersAsync(bool isAdmin, string userId)
        {
            IEnumerable<OrderHeader> orders;

            if (isAdmin)
                orders = await _unitOfWork.Order.GetAllWithDetailsAsync();
            else
                orders = await _unitOfWork.Order.GetByUserIdAsync(userId);

            return _mapper.Map<IEnumerable<OrderHeaderDto>>(orders);
        }

        public async Task<OrderHeaderDto?> GetOrderByIdAsync(int id)
        {
            var order = await _unitOfWork.Order.GetWithDetailsAsync(id);
            return order == null ? null : _mapper.Map<OrderHeaderDto>(order);
        }

        public async Task<OrderHeaderDto> CreateOrderAsync(CartDto cartDto)
        {
            OrderHeaderDto orderHeaderDto = _mapper.Map<OrderHeaderDto>(cartDto.CartHeader);
            orderHeaderDto.OrderTime = DateTime.Now;
            orderHeaderDto.Status = SD.Status_Pending;
            orderHeaderDto.OrderDetails = _mapper.Map<IEnumerable<OrderDetailsDto>>(cartDto.CartDetails);
            orderHeaderDto.OrderTotal = Math.Round(orderHeaderDto.OrderTotal, 2);

            var orderHeader = _mapper.Map<OrderHeader>(orderHeaderDto);
            await _unitOfWork.Order.CreateAsync(orderHeader);
            await _unitOfWork.SaveAsync();

            orderHeaderDto.OrderHeaderId = orderHeader.OrderHeaderId;
            return orderHeaderDto;
        }

        public async Task<StripeRequestDto> CreateStripeSessionAsync(StripeRequestDto stripeRequestDto)
        {
            var options = new SessionCreateOptions
            {
                SuccessUrl = stripeRequestDto.ApprovedUrl,
                CancelUrl = stripeRequestDto.CancelUrl,
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            var discountsObj = new List<SessionDiscountOptions>
            {
                new SessionDiscountOptions { Coupon = stripeRequestDto.OrderHeader.CouponCode }
            };

            foreach (var item in stripeRequestDto.OrderHeader.OrderDetails)
            {
                options.LineItems.Add(new SessionLineItemOptions
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
                });
            }

            if (stripeRequestDto.OrderHeader.Discount > 0)
                options.Discounts = discountsObj;

            var service = new SessionService();
            Session session = service.Create(options);
            stripeRequestDto.StripeSessionUrl = session.Url;

            var order = await _unitOfWork.Order.GetAsync(o => o.OrderHeaderId == stripeRequestDto.OrderHeader.OrderHeaderId);
            if (order is not null)
            {
                order.StripeSessionId = session.Id;
                await _unitOfWork.Order.UpdateAsync(order);
                await _unitOfWork.SaveAsync();
            }

            return stripeRequestDto;
        }

        public async Task<OrderHeaderDto> ValidateStripeSessionAsync(int orderHeaderId)
        {
            var orderHeader = await _unitOfWork.Order.GetAsync(o => o.OrderHeaderId == orderHeaderId)
                ?? throw new KeyNotFoundException("Order not found");

            var sessionService = new SessionService();
            Session session = sessionService.Get(orderHeader.StripeSessionId);

            var paymentIntentService = new PaymentIntentService();
            PaymentIntent paymentIntent = paymentIntentService.Get(session.PaymentIntentId);

            if (paymentIntent.Status == "succeeded")
            {
                orderHeader.PaymentIntentId = paymentIntent.Id;
                orderHeader.Status = SD.Status_Approved;
                await _unitOfWork.Order.UpdateAsync(orderHeader);
                await _unitOfWork.SaveAsync();

                RewardsDto rewardsDto = new()
                {
                    OrderId = orderHeader.OrderHeaderId,
                    RewardsActivity = Convert.ToInt32(orderHeader.OrderTotal),
                    UserId = orderHeader.UserId
                };

                string topicName = _configuration.GetValue<string>("RabbitMQ:OrderCreatedQueue") ?? "OrderCreatedQueue";
                await _messageBus.PublishMessage(rewardsDto, topicName);
            }

            return _mapper.Map<OrderHeaderDto>(orderHeader);
        }

        public async Task UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await _unitOfWork.Order.GetAsync(o => o.OrderHeaderId == orderId)
                ?? throw new KeyNotFoundException("Order not found");

            if (newStatus == SD.Status_Cancelled)
            {
                if (string.IsNullOrEmpty(order.PaymentIntentId))
                    throw new InvalidOperationException("No payment found to refund");

                var refundService = new RefundService();
                refundService.Create(new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = order.PaymentIntentId
                });
            }

            order.Status = newStatus;
            await _unitOfWork.Order.UpdateAsync(order);
            await _unitOfWork.SaveAsync();
        }
    }
}
