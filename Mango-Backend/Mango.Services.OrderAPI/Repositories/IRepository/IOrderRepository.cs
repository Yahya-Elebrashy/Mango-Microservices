using Mango.Services.OrderAPI.Models;

namespace Mango.Services.OrderAPI.Repositories.IRepository;

public interface IOrderRepository : IRepository<OrderHeader>
{
    Task<IEnumerable<OrderHeader>> GetAllWithDetailsAsync();
    Task<OrderHeader?> GetWithDetailsAsync(int orderHeaderId);
    Task<IEnumerable<OrderHeader>> GetByUserIdAsync(string userId);
}
