using Mango.Services.ShoppingCartAPI.Models;

namespace Mango.Services.ShoppingCartAPI.Repositories.IRepository;

public interface ICartDetailsRepository : IRepository<CartDetails>
{
    Task<int> CountByHeaderIdAsync(int cartHeaderId);
}
