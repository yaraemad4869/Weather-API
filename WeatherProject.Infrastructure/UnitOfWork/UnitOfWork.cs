using Microsoft.EntityFrameworkCore.Storage;
using WeatherProject.Core.Entities;
using WeatherProject.Core.Interfaces;
using WeatherProject.Infrastructure.Data;
using WeatherProject.Infrastructure.Repositories;

namespace WeatherProject.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;
    
    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }
    
    private IRepository<City>? _cities;
    private IRepository<WeatherData>? _weatherData;
    private IRepository<WeatherCondition>? _weatherConditions;
    
    public IRepository<City> Cities => _cities ??= new Repository<City>(_context);
    public IRepository<WeatherData> WeatherData => _weatherData ??= new Repository<WeatherData>(_context);
    public IRepository<WeatherCondition> WeatherConditions => _weatherConditions ??= new Repository<WeatherCondition>(_context);
    
    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }
    
    public async Task CommitTransactionAsync()
    {
        try
        {
            await _transaction?.CommitAsync();
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
    
    public async Task RollbackTransactionAsync()
    {
        try
        {
            await _transaction?.RollbackAsync();
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
            _transaction?.Dispose();
        }
        _disposed = true;
    }
}