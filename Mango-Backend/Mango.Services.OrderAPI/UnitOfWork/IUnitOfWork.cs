using Mango.Services.OrderAPI.Repositories.IRepository;

namespace Mango.Services.OrderAPI.UnitOfWork;

public interface IUnitOfWork
{
    IOrderRepository Order { get; }
    Task SaveAsync();
}
