using AutoMapper;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Mango.Services.ShoppingCartAPI.UnitOfWork;
using MessageBus;

namespace Mango.Services.ShoppingCartAPI.Service
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IProductService _productService;
        private readonly ICouponService _couponService;
        private readonly IMessageBus _messageBus;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CartService> _logger;

        public CartService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IProductService productService,
            ICouponService couponService,
            IMessageBus messageBus,
            IConfiguration configuration,
            ILogger<CartService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _productService = productService;
            _couponService = couponService;
            _messageBus = messageBus;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CartDto> GetCartAsync(string userId)
        {
            _logger.LogInformation("Fetching cart for user {UserId}", userId);

            var cartHeader = await _unitOfWork.CartHeader.GetAsync(c => c.UserId == userId);
            if (cartHeader == null)
            {
                _logger.LogWarning("Cart not found for user {UserId}", userId);
                throw new KeyNotFoundException("Cart not found");
            }

            var cartDetails = await _unitOfWork.CartDetails
                .GetAllAsync()
                .ContinueWith(t => t.Result.Where(c => c.CartHeaderId == cartHeader.CartHeaderId));

            var cartDto = new CartDto
            {
                CartHeader = _mapper.Map<CartHeaderDto>(cartHeader),
                CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(cartDetails)
            };

            _logger.LogInformation("Cart for user {UserId} has {ItemCount} item(s). CartHeaderId={CartHeaderId}",
                userId, cartDto.CartDetails.Count(), cartHeader.CartHeaderId);

            _logger.LogDebug("Fetching product details to populate cart for user {UserId}", userId);
            var products = await _productService.GetProductsAsync();

            foreach (var item in cartDto.CartDetails)
            {
                item.ProductDto = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                cartDto.CartHeader.CartTotal += item.Count * item.ProductDto?.Price ?? 0;
            }

            _logger.LogDebug("Cart total before coupon for user {UserId}: {CartTotal}", userId, cartDto.CartHeader.CartTotal);

            if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
            {
                _logger.LogInformation("Applying coupon {CouponCode} to cart for user {UserId}",
                    cartDto.CartHeader.CouponCode, userId);

                var coupon = await _couponService.GetCouponAsync(cartDto.CartHeader.CouponCode);

                if (coupon != null && cartDto.CartHeader.CartTotal >= coupon.MinAmount)
                {
                    cartDto.CartHeader.CartTotal -= coupon.DiscountAmount;
                    cartDto.CartHeader.Discount = coupon.DiscountAmount;

                    _logger.LogInformation(
                        "Coupon {CouponCode} applied to cart for user {UserId}. Discount={Discount}, NewTotal={CartTotal}",
                        cartDto.CartHeader.CouponCode, userId, coupon.DiscountAmount, cartDto.CartHeader.CartTotal);
                }
                else
                {
                    _logger.LogWarning(
                        "Coupon {CouponCode} not applied for user {UserId}. Reason: {Reason}",
                        cartDto.CartHeader.CouponCode, userId,
                        coupon == null ? "Coupon not found" : $"Cart total {cartDto.CartHeader.CartTotal} below minimum {coupon.MinAmount}");
                }
            }

            cartDto.CartHeader.CartTotal = Math.Round(cartDto.CartHeader.CartTotal, 2);

            _logger.LogInformation("Cart retrieved for user {UserId}. FinalTotal={CartTotal}",
                userId, cartDto.CartHeader.CartTotal);

            return cartDto;
        }

        public async Task<CartDto> UpsertCartAsync(CartDto cartDto)
        {
            var userId = cartDto.CartHeader.UserId;
            var incomingProductId = cartDto.CartDetails.First().ProductId;
            var incomingCount = cartDto.CartDetails.First().Count;

            _logger.LogInformation("Upserting cart for user {UserId}. ProductId={ProductId}, Count={Count}",
                userId, incomingProductId, incomingCount);

            var cartHeaderFromDb = await _unitOfWork.CartHeader.GetAsync(c => c.UserId == userId);

            if (cartHeaderFromDb == null)
            {
                _logger.LogInformation("No existing cart for user {UserId} — creating new cart header and detail", userId);

                var newHeader = _mapper.Map<CartHeader>(cartDto.CartHeader);
                await _unitOfWork.CartHeader.CreateAsync(newHeader);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("New cart header created. CartHeaderId={CartHeaderId}, UserId={UserId}",
                    newHeader.CartHeaderId, userId);

                cartDto.CartDetails.First().CartHeaderId = newHeader.CartHeaderId;
                await _unitOfWork.CartDetails.CreateAsync(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("New cart detail added. CartHeaderId={CartHeaderId}, ProductId={ProductId}, Count={Count}",
                    newHeader.CartHeaderId, incomingProductId, incomingCount);
            }
            else
            {
                _logger.LogInformation("Existing cart found for user {UserId}. CartHeaderId={CartHeaderId}",
                    userId, cartHeaderFromDb.CartHeaderId);

                var cartDetailsFromDb = await _unitOfWork.CartDetails.GetAsync(
                    c => c.ProductId == incomingProductId && c.CartHeaderId == cartHeaderFromDb.CartHeaderId);

                if (cartDetailsFromDb == null)
                {
                    _logger.LogInformation("Product {ProductId} not in cart — adding new detail. CartHeaderId={CartHeaderId}",
                        incomingProductId, cartHeaderFromDb.CartHeaderId);

                    cartDto.CartDetails.First().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                    await _unitOfWork.CartDetails.CreateAsync(_mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                }
                else
                {
                    var previousCount = cartDetailsFromDb.Count;
                    cartDetailsFromDb.Count += incomingCount;
                    await _unitOfWork.CartDetails.UpdateAsync(cartDetailsFromDb);

                    _logger.LogInformation(
                        "Product {ProductId} already in cart — updated count from {PreviousCount} to {NewCount}. CartHeaderId={CartHeaderId}",
                        incomingProductId, previousCount, cartDetailsFromDb.Count, cartHeaderFromDb.CartHeaderId);
                }

                await _unitOfWork.SaveAsync();
            }

            _logger.LogInformation("Cart upsert completed for user {UserId}. ProductId={ProductId}",
                userId, incomingProductId);

            return cartDto;
        }

        public async Task RemoveCartItemAsync(int cartDetailsId)
        {
            _logger.LogInformation("Removing cart item. CartDetailsId={CartDetailsId}", cartDetailsId);

            var cartDetails = await _unitOfWork.CartDetails.GetAsync(c => c.CartDetailsId == cartDetailsId);
            if (cartDetails == null)
            {
                _logger.LogWarning("Remove failed — cart item not found. CartDetailsId={CartDetailsId}", cartDetailsId);
                throw new KeyNotFoundException("Item not found");
            }

            var cartHeaderId = cartDetails.CartHeaderId;
            int remainingItems = await _unitOfWork.CartDetails.CountByHeaderIdAsync(cartHeaderId);

            await _unitOfWork.CartDetails.RemoveAsync(cartDetails);

            _logger.LogInformation("Cart item removed. CartDetailsId={CartDetailsId}, CartHeaderId={CartHeaderId}",
                cartDetailsId, cartHeaderId);

            if (remainingItems == 1)
            {
                _logger.LogInformation("Last item removed from cart — deleting cart header. CartHeaderId={CartHeaderId}",
                    cartHeaderId);

                var header = await _unitOfWork.CartHeader.GetAsync(c => c.CartHeaderId == cartHeaderId);
                if (header != null)
                {
                    await _unitOfWork.CartHeader.RemoveAsync(header);
                    _logger.LogInformation("Cart header deleted. CartHeaderId={CartHeaderId}", cartHeaderId);
                }
                else
                {
                    _logger.LogWarning("Cart header not found during cleanup. CartHeaderId={CartHeaderId}", cartHeaderId);
                }
            }
            else
            {
                _logger.LogDebug("Cart still has {RemainingItems} item(s) after removal. CartHeaderId={CartHeaderId}",
                    remainingItems - 1, cartHeaderId);
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task ApplyCouponAsync(string userId, string couponCode)
        {
            _logger.LogInformation("Applying coupon {CouponCode} to cart for user {UserId}", couponCode, userId);

            var cartFromDb = await _unitOfWork.CartHeader.GetAsync(c => c.UserId == userId);
            if (cartFromDb == null)
            {
                _logger.LogWarning("Apply coupon failed — cart not found for user {UserId}", userId);
                throw new KeyNotFoundException("Cart not found");
            }

            var coupon = await _couponService.GetCouponAsync(couponCode);
            if (coupon == null || string.IsNullOrEmpty(coupon.CouponCode))
            {
                _logger.LogWarning("Apply coupon failed — coupon {CouponCode} is invalid or not found. UserId={UserId}",
                    couponCode, userId);
                throw new ArgumentException("Invalid coupon code");
            }

            cartFromDb.CouponCode = couponCode;
            await _unitOfWork.CartHeader.UpdateAsync(cartFromDb);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Coupon {CouponCode} applied successfully to cart for user {UserId}. CartHeaderId={CartHeaderId}",
                couponCode, userId, cartFromDb.CartHeaderId);
        }

        public async Task RemoveCouponAsync(string userId)
        {
            _logger.LogInformation("Removing coupon from cart for user {UserId}", userId);

            var cartFromDb = await _unitOfWork.CartHeader.GetAsync(c => c.UserId == userId);
            if (cartFromDb == null)
            {
                _logger.LogWarning("Remove coupon failed — cart not found for user {UserId}", userId);
                throw new KeyNotFoundException("Cart not found");
            }

            var previousCoupon = cartFromDb.CouponCode;
            cartFromDb.CouponCode = "";
            await _unitOfWork.CartHeader.UpdateAsync(cartFromDb);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Coupon {PreviousCouponCode} removed from cart for user {UserId}. CartHeaderId={CartHeaderId}",
                previousCoupon, userId, cartFromDb.CartHeaderId);
        }

        public async Task EmailCartRequestAsync(CartDto cartDto)
        {
            var userId = cartDto.CartHeader.UserId;
            var queueName = _configuration.GetValue<string>("RabbitMQ:EmailQueue") ?? "emailcartqueue";

            _logger.LogInformation("Publishing email cart request to queue {QueueName} for user {UserId}", queueName, userId);

            await _messageBus.PublishMessage(cartDto, queueName);

            _logger.LogInformation("Email cart request published successfully to queue {QueueName} for user {UserId}",
                queueName, userId);
        }
    }
}