using Mango.Services.ProductAPI.Repositories.IRepository;

namespace Mango.Services.ProductAPI.UnitOfWork;

public interface IUnitOfWork
{
    IProductRepository Product { get; }
    Task SaveAsync();
}
