using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WeatherProject.Core.Interfaces.Repositories;
using WeatherProject.Infrastructure.Data;

public class WeatherRepository<T> : IWeatherRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public WeatherRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    public IQueryable<T> Query() => _dbSet.AsQueryable();
    
    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
    
    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
    
    public async Task AddAsync(T entity, CancellationToken ct = default) 
        => await _dbSet.AddAsync(entity, ct);
    
    public void Update(T entity) 
        => _dbSet.Update(entity);
    
    public void Delete(T entity) 
        => _dbSet.Remove(entity);
    
    public async Task<int> SaveChangesAsync(CancellationToken ct = default) 
        => await _context.SaveChangesAsync(ct);
}