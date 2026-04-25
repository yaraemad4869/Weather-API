using WeatherProject.Core.Entities;

namespace WeatherProject.Core.Interfaces
{

    public interface IUnitOfWork : IDisposable
    {
        IRepository<City> Cities { get; }
        IRepository<WeatherData> WeatherData { get; }
        IRepository<WeatherCondition> WeatherConditions { get; }

        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}