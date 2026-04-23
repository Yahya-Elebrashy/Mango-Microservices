using Mango.Services.ShoppingCartAPI.Models.Dto;

namespace Mango.Services.ShoppingCartAPI.Service.IService
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(string userId);
        Task<CartDto> UpsertCartAsync(CartDto cartDto);
        Task RemoveCartItemAsync(int cartDetailsId);
        Task RemoveCouponAsync(string userId);
        Task ApplyCouponAsync(string userId, string couponCode);
        Task EmailCartRequestAsync(CartDto cartDto);
    }
}
