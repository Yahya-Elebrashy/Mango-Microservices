using Mango.Services.ShoppingCartAPI.Repositories.IRepository;

namespace Mango.Services.ShoppingCartAPI.UnitOfWork;

public interface IUnitOfWork
{
    ICartHeaderRepository CartHeader { get; }
    ICartDetailsRepository CartDetails { get; }
    Task SaveAsync();
}
