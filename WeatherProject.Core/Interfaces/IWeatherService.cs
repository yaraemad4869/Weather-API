using WeatherProject.Core.Entities;

namespace WeatherProject.Core.Interfaces;

public interface IWeatherService
{
    Task<WeatherData?> GetCurrentWeatherAsync(int cityId);
    Task<IEnumerable<WeatherData>> GetWeatherForecastAsync(int cityId, int days);
    Task<IEnumerable<WeatherData>> GetHistoricalWeatherAsync(int cityId, DateTime startDate, DateTime endDate);
    Task<WeatherStatistics> GetWeatherStatisticsAsync(int cityId, DateTime startDate, DateTime endDate);
    Task<bool> FetchAndStoreWeatherDataAsync(int cityId);
    Task FetchAndStoreAllCitiesWeatherAsync();
}

public class WeatherStatistics
{
    public decimal AverageTemperature { get; set; }
    public decimal MaxTemperature { get; set; }
    public decimal MinTemperature { get; set; }
    public decimal AverageHumidity { get; set; }
    public decimal AverageWindSpeed { get; set; }
    public decimal TotalPrecipitation { get; set; }
    public string? MostCommonCondition { get; set; }
    public int TotalRecords { get; set; }
}