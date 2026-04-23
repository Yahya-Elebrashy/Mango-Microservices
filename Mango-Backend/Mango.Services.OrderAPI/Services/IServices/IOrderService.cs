using Mango.Services.OrderAPI.Models.Dto;

namespace Mango.Services.OrderAPI.Services.IServices
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderHeaderDto>> GetAllOrdersAsync(bool isAdmin, string userId);
        Task<OrderHeaderDto?> GetOrderByIdAsync(int id);
        Task<OrderHeaderDto> CreateOrderAsync(CartDto cartDto);
        Task<StripeRequestDto> CreateStripeSessionAsync(StripeRequestDto stripeRequestDto);
        Task<OrderHeaderDto> ValidateStripeSessionAsync(int orderHeaderId);
        Task UpdateOrderStatusAsync(int orderId, string newStatus);
    }
}
