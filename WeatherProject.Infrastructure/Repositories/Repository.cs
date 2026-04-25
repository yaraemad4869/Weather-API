using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WeatherProject.Core.Interfaces;
using WeatherProject.Infrastructure.Data;

namespace WeatherProject.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        return entities;
    }

    public virtual Task<T> UpdateAsync(T entity)
    {
        // Check if entity is already tracked
        var entry = _context.Entry(entity);

        if (entry.State == EntityState.Detached)
        {
            // Try to find the tracked entity
            var keyName = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties.FirstOrDefault()?.Name;
            if (keyName != null)
            {
                var keyValue = entity.GetType().GetProperty(keyName)?.GetValue(entity);
                if (keyValue != null)
                {
                    var trackedEntity = _dbSet.Local.FirstOrDefault(e =>
                        e.GetType().GetProperty(keyName)?.GetValue(e)?.Equals(keyValue) == true);

                    if (trackedEntity != null)
                    {
                        // Update the tracked entity instead of attaching a new one
                        _context.Entry(trackedEntity).CurrentValues.SetValues(entity);
                        return Task.FromResult(trackedEntity);
                    }
                }
            }

            // If not tracked, attach and set to Modified
            _dbSet.Attach(entity);
            entry.State = EntityState.Modified;
        }

        return Task.FromResult(entity);
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        _dbSet.Remove(entity);
        return true;
    }

    public virtual Task<bool> DeleteRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
        return Task.FromResult(true);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null)
            return await _dbSet.CountAsync();

        return await _dbSet.CountAsync(predicate);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
    {
        IQueryable<T> query = _dbSet;

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }
}