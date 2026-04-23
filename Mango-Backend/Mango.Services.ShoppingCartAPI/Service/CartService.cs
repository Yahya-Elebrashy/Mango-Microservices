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

        public CartService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IProductService productService,
            ICouponService couponService,
            IMessageBus messageBus,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _productService = productService;
            _couponService = couponService;
            _messageBus = messageBus;
            _configuration = configuration;
        }

        public async Task<CartDto> GetCartAsync(string userId)
        {
            var cartHeader = await _unitOfWork.CartHeader.GetAsync(c => c.UserId == userId)
                ?? throw new KeyNotFoundException("Cart not found");

            var cartDetails = await _unitOfWork.CartDetails
                .GetAllAsync()
                .ContinueWith(t => t.Result.Where(c => c.CartHeaderId == cartHeader.CartHeaderId));

            var cartDto = new CartDto
            {
                CartHeader = _mapper.Map<CartHeaderDto>(cartHeader),
                CartDetails = _mapper.Map<IEnumerable<CartDetailsDto>>(cartDetails)
            };

            var products = await _productService.GetProductsAsync();
            foreach (var item in cartDto.CartDetails)
            {
                item.ProductDto = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                cartDto.CartHeader.CartTotal += item.Count * item.ProductDto?.Price ?? 0;
            }

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
            return cartDto;
        }

        public async Task<CartDto> UpsertCartAsync(CartDto cartDto)
        {
            var cartHeaderFromDb = await _unitOfWork.CartHeader
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
                var cartDetailsFromDb = await _unitOfWork.CartDetails.GetAsync(
                    c => c.ProductId == cartDto.CartDetails.First().ProductId
                    && c.CartHeaderId == cartHeaderFromDb.CartHeaderId);

                if (cartDetailsFromDb == null)
                {
                    cartDto.CartDetails.First().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                    await _unitOfWork.CartDetails.CreateAsync(
                        _mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                }
                else
                {
                    cartDto.CartDetails.First().Count += cartDetailsFromDb.Count;
                    cartDto.CartDetails.First().CartHeaderId = cartDetailsFromDb.CartHeaderId;
                    cartDto.CartDetails.First().CartDetailsId = cartDetailsFromDb.CartDetailsId;
                    await _unitOfWork.CartDetails.UpdateAsync(
                        _mapper.Map<CartDetails>(cartDto.CartDetails.First()));
                }
                await _unitOfWork.SaveAsync();
            }

            return cartDto;
        }

        public async Task RemoveCartItemAsync(int cartDetailsId)
        {
            var cartDetails = await _unitOfWork.CartDetails
                .GetAsync(c => c.CartDetailsId == cartDetailsId)
                ?? throw new KeyNotFoundException("Item not found");

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
        }

        public async Task ApplyCouponAsync(string userId, string couponCode)
        {
            var cartFromDb = await _unitOfWork.CartHeader.GetAsync(c => c.UserId == userId)
                ?? throw new KeyNotFoundException("Cart not found");

            var coupon = await _couponService.GetCouponAsync(couponCode);
            if (coupon == null || string.IsNullOrEmpty(coupon.CouponCode))
                throw new ArgumentException("Invalid coupon code");

            cartFromDb.CouponCode = couponCode;
            await _unitOfWork.CartHeader.UpdateAsync(cartFromDb);
            await _unitOfWork.SaveAsync();
        }

        public async Task RemoveCouponAsync(string userId)
        {
            var cartFromDb = await _unitOfWork.CartHeader.GetAsync(c => c.UserId == userId)
                ?? throw new KeyNotFoundException("Cart not found");

            cartFromDb.CouponCode = "";
            await _unitOfWork.CartHeader.UpdateAsync(cartFromDb);
            await _unitOfWork.SaveAsync();
        }

        public async Task EmailCartRequestAsync(CartDto cartDto)
        {
            var queueName = _configuration.GetValue<string>("RabbitMQ:EmailQueue") ?? "emailcartqueue";
            await _messageBus.PublishMessage(cartDto, queueName);
        }
    }
}
