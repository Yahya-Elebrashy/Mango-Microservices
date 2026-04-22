using System.Linq.Expressions;
using Mango.Services.CouponAPI.Data;
using Mango.Services.CouponAPI.Repositories.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.CouponAPI.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _db;
    internal DbSet<T> dbSet;

    public Repository(AppDbContext db)
    {
        _db = db;
        dbSet = _db.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await dbSet.ToListAsync();

    public async Task<T?> GetAsync(Expression<Func<T, bool>> filter) =>
        await dbSet.Where(filter).FirstOrDefaultAsync();

    public async Task CreateAsync(T entity) =>
        await dbSet.AddAsync(entity);

    public async Task UpdateAsync(T entity) =>
        dbSet.Update(entity);

    public async Task RemoveAsync(T entity) =>
        dbSet.Remove(entity);
}
