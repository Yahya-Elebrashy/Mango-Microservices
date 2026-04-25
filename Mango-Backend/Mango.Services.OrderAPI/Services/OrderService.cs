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
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageBus messageBus,
            IConfiguration configuration,
            ILogger<OrderService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageBus = messageBus;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IEnumerable<OrderHeaderDto>> GetAllOrdersAsync(bool isAdmin, string userId)
        {
            if (isAdmin)
                _logger.LogInformation("Admin requested all orders");
            else
                _logger.LogInformation("User {UserId} requested their orders", userId);

            var orders = isAdmin
                ? await _unitOfWork.Order.GetAllWithDetailsAsync()
                : await _unitOfWork.Order.GetByUserIdAsync(userId);

            var result = _mapper.Map<IEnumerable<OrderHeaderDto>>(orders);

            _logger.LogInformation("Retrieved {Count} orders for {Scope}",
                result.Count(), isAdmin ? "admin" : $"user {userId}");

            return result;
        }

        public async Task<OrderHeaderDto?> GetOrderByIdAsync(int id)
        {
            _logger.LogInformation("Fetching order with ID {OrderId}", id);

            var order = await _unitOfWork.Order.GetWithDetailsAsync(id);

            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} was not found", id);
                return null;
            }

            _logger.LogInformation("Successfully retrieved order {OrderId} for user {UserId}",
                id, order.UserId);
            return _mapper.Map<OrderHeaderDto>(order);
        }

        public async Task<OrderHeaderDto> CreateOrderAsync(CartDto cartDto)
        {
            _logger.LogInformation("Creating order for user {UserId} with {ItemCount} item(s)",
                cartDto.CartHeader.UserId, cartDto.CartDetails?.Count());

            var orderHeaderDto = _mapper.Map<OrderHeaderDto>(cartDto.CartHeader);
            orderHeaderDto.OrderTime = DateTime.Now;
            orderHeaderDto.Status = SD.Status_Pending;
            orderHeaderDto.OrderDetails = _mapper.Map<IEnumerable<OrderDetailsDto>>(cartDto.CartDetails);
            orderHeaderDto.OrderTotal = Math.Round(orderHeaderDto.OrderTotal, 2);

            var orderHeader = _mapper.Map<OrderHeader>(orderHeaderDto);

            await _unitOfWork.Order.CreateAsync(orderHeader);
            await _unitOfWork.SaveAsync();

            orderHeaderDto.OrderHeaderId = orderHeader.OrderHeaderId;

            _logger.LogInformation(
                "Order {OrderId} created successfully for user {UserId}. Status={Status}, Total={OrderTotal}",
                orderHeader.OrderHeaderId, orderHeader.UserId, orderHeader.Status, orderHeaderDto.OrderTotal);

            return orderHeaderDto;
        }

        public async Task<StripeRequestDto> CreateStripeSessionAsync(StripeRequestDto stripeRequestDto)
        {
            var orderId = stripeRequestDto.OrderHeader.OrderHeaderId;
            var couponCode = stripeRequestDto.OrderHeader.CouponCode;
            var discount = stripeRequestDto.OrderHeader.Discount;
            var lineItemCount = stripeRequestDto.OrderHeader.OrderDetails?.Count();

            _logger.LogInformation(
                "Creating Stripe session for order {OrderId}. LineItems={LineItemCount}, Discount={Discount}, CouponCode={CouponCode}",
                orderId, lineItemCount, discount, couponCode);

            var options = new SessionCreateOptions
            {
                SuccessUrl = stripeRequestDto.ApprovedUrl,
                CancelUrl = stripeRequestDto.CancelUrl,
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            var discountsObj = new List<SessionDiscountOptions>
            {
                new SessionDiscountOptions { Coupon = couponCode }
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

            if (discount > 0)
            {
                options.Discounts = discountsObj;
                _logger.LogInformation("Applying coupon {CouponCode} to Stripe session for order {OrderId}",
                    couponCode, orderId);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            _logger.LogInformation("Stripe session created. SessionId={StripeSessionId}, OrderId={OrderId}",
                session.Id, orderId);

            stripeRequestDto.StripeSessionUrl = session.Url;

            var order = await _unitOfWork.Order.GetAsync(o => o.OrderHeaderId == orderId);
            if (order is not null)
            {
                order.StripeSessionId = session.Id;
                await _unitOfWork.Order.UpdateAsync(order);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Stripe session ID {StripeSessionId} persisted to order {OrderId}",
                    session.Id, orderId);
            }
            else
            {
                _logger.LogWarning("Order {OrderId} not found when persisting Stripe session ID {StripeSessionId}",
                    orderId, session.Id);
            }

            return stripeRequestDto;
        }

        public async Task<OrderHeaderDto> ValidateStripeSessionAsync(int orderHeaderId)
        {
            _logger.LogInformation("Validating Stripe session for order {OrderId}", orderHeaderId);

            var orderHeader = await _unitOfWork.Order.GetAsync(o => o.OrderHeaderId == orderHeaderId);
            if (orderHeader == null)
            {
                _logger.LogWarning("Validation failed — order {OrderId} not found", orderHeaderId);
                throw new KeyNotFoundException("Order not found");
            }

            _logger.LogDebug("Retrieving Stripe session {StripeSessionId} for order {OrderId}",
                orderHeader.StripeSessionId, orderHeaderId);

            var sessionService = new SessionService();
            Session session = sessionService.Get(orderHeader.StripeSessionId);

            var paymentIntentService = new PaymentIntentService();
            PaymentIntent paymentIntent = paymentIntentService.Get(session.PaymentIntentId);

            _logger.LogInformation(
                "Stripe payment intent status: {PaymentStatus}. PaymentIntentId={PaymentIntentId}, OrderId={OrderId}",
                paymentIntent.Status, paymentIntent.Id, orderHeaderId);

            if (paymentIntent.Status == "succeeded")
            {
                orderHeader.PaymentIntentId = paymentIntent.Id;
                orderHeader.Status = SD.Status_Approved;

                await _unitOfWork.Order.UpdateAsync(orderHeader);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation(
                    "Order {OrderId} approved. PaymentIntentId={PaymentIntentId}, UserId={UserId}, Total={OrderTotal}",
                    orderHeaderId, paymentIntent.Id, orderHeader.UserId, orderHeader.OrderTotal);

                var rewardsDto = new RewardsDto
                {
                    OrderId = orderHeader.OrderHeaderId,
                    RewardsActivity = Convert.ToInt32(orderHeader.OrderTotal),
                    UserId = orderHeader.UserId
                };

                string topicName = _configuration.GetValue<string>("RabbitMQ:OrderCreatedQueue") ?? "OrderCreatedQueue";

                _logger.LogInformation(
                    "Publishing rewards message to queue {QueueName} for order {OrderId}. RewardsPoints={RewardsPoints}, UserId={UserId}",
                    topicName, orderHeaderId, rewardsDto.RewardsActivity, rewardsDto.UserId);

                await _messageBus.PublishMessage(rewardsDto, topicName);

                _logger.LogInformation("Rewards message published successfully for order {OrderId}", orderHeaderId);
            }
            else
            {
                _logger.LogWarning(
                    "Payment not succeeded for order {OrderId}. PaymentStatus={PaymentStatus}, PaymentIntentId={PaymentIntentId}",
                    orderHeaderId, paymentIntent.Status, paymentIntent.Id);
            }

            return _mapper.Map<OrderHeaderDto>(orderHeader);
        }

        public async Task UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            _logger.LogInformation("Updating status for order {OrderId} to {NewStatus}", orderId, newStatus);

            var order = await _unitOfWork.Order.GetAsync(o => o.OrderHeaderId == orderId);
            if (order == null)
            {
                _logger.LogWarning("Status update failed — order {OrderId} not found", orderId);
                throw new KeyNotFoundException("Order not found");
            }

            var previousStatus = order.Status;

            if (newStatus == SD.Status_Cancelled)
            {
                if (string.IsNullOrEmpty(order.PaymentIntentId))
                {
                    _logger.LogWarning(
                        "Cancellation failed for order {OrderId} — no PaymentIntentId on record",
                        orderId);
                    throw new InvalidOperationException("No payment found to refund");
                }

                _logger.LogInformation(
                    "Initiating Stripe refund for order {OrderId}. PaymentIntentId={PaymentIntentId}",
                    orderId, order.PaymentIntentId);

                var refundService = new RefundService();
                var refund = refundService.Create(new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = order.PaymentIntentId
                });

                _logger.LogInformation(
                    "Stripe refund issued for order {OrderId}. RefundId={RefundId}, PaymentIntentId={PaymentIntentId}",
                    orderId, refund.Id, order.PaymentIntentId);
            }

            order.Status = newStatus;
            await _unitOfWork.Order.UpdateAsync(order);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "Order {OrderId} status updated from {PreviousStatus} to {NewStatus}",
                orderId, previousStatus, newStatus);
        }
    }
}