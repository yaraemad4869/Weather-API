namespace WeatherProject.Core.Interfaces.Repositories
{
    public interface IWeatherRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity, CancellationToken ct = default);
        void Update(T entity);
        void Delete(T entity);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        IQueryable<T> Query(); // لدعم الـ Specification Pattern
    }
}