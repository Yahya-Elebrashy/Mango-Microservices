using System.Linq.Expressions;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Repositories.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ProductAPI.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _db;
    internal DbSet<T> dbSet;

    public Repository(AppDbContext db)
    {
        _db = db;
        dbSet = _db.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await dbSet.ToListAsync();
    }

    public async Task<T?> GetAsync(Expression<Func<T, bool>> filter)
    {
        return await dbSet.Where(filter).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(T entity)
    {
        await dbSet.AddAsync(entity);
    }

    public async Task UpdateAsync(T entity)
    {
        dbSet.Update(entity);
    }

    public async Task RemoveAsync(T entity)
    {
        dbSet.Remove(entity);
    }
}
