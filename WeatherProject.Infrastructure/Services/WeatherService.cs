using Microsoft.Extensions.Logging;
using WeatherProject.Core.Interfaces;
using WeatherProject.Core.Entities;

namespace WeatherProject.Infrastructure.Services;

public class WeatherService : IWeatherService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly OpenMeteoService _openMeteoService;
    private readonly ILogger<WeatherService> _logger;
    
    public WeatherService(IUnitOfWork unitOfWork, OpenMeteoService openMeteoService, ILogger<WeatherService> logger)
    {
        _unitOfWork = unitOfWork;
        _openMeteoService = openMeteoService;
        _logger = logger;
    }
    
    public async Task<WeatherData?> GetCurrentWeatherAsync(int cityId)
    {
        var city = await _unitOfWork.Cities.GetByIdAsync(cityId);
        if (city == null)
            throw new Core.Exceptions.WeatherException($"City with ID {cityId} not found", 404);
        
        return (await _unitOfWork.WeatherData.FindAsync(w => w.CityId == cityId && w.Timestamp.Date == DateTime.UtcNow.Date))
            .OrderByDescending(w => w.Timestamp)
            .FirstOrDefault();
    }
    
    public async Task<IEnumerable<WeatherData>> GetWeatherForecastAsync(int cityId, int days)
    {
        var city = await _unitOfWork.Cities.GetByIdAsync(cityId);
        if (city == null)
            throw new Core.Exceptions.WeatherException($"City with ID {cityId} not found", 404);
        
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(days);
        
        return await _unitOfWork.WeatherData.FindAsync(w => w.CityId == cityId && w.Timestamp >= startDate && w.Timestamp <= endDate);
    }
    
    public async Task<IEnumerable<WeatherData>> GetHistoricalWeatherAsync(int cityId, DateTime startDate, DateTime endDate)
    {
        var city = await _unitOfWork.Cities.GetByIdAsync(cityId);
        if (city == null)
            throw new Core.Exceptions.WeatherException($"City with ID {cityId} not found", 404);
        
        return await _unitOfWork.WeatherData.FindAsync(w => w.CityId == cityId && w.Timestamp >= startDate && w.Timestamp <= endDate);
    }
    public async Task<WeatherStatistics> GetWeatherStatisticsAsync(int cityId, DateTime startDate, DateTime endDate)
    {
        var city = await _unitOfWork.Cities.GetByIdAsync(cityId);
        if (city == null)
            throw new Core.Exceptions.WeatherException($"City with ID {cityId} not found", 404);

        // Ensure dates are in UTC
        startDate = startDate.ToUniversalTime();
        endDate = endDate.ToUniversalTime().AddDays(1); // Include the end date

        _logger.LogInformation("Getting statistics for city {CityName} from {StartDate} to {EndDate}",
            city.Name, startDate, endDate);

        var weatherData = (await _unitOfWork.WeatherData.FindAsync(w =>
            w.CityId == cityId && w.Timestamp >= startDate && w.Timestamp <= endDate))
            .ToList();

        _logger.LogInformation("Found {Count} weather records for city {CityName}", weatherData.Count, city.Name);

        if (!weatherData.Any())
        {
            _logger.LogWarning("No weather data found for city {CityName} in the specified date range", city.Name);
            return new WeatherStatistics
            {
                AverageTemperature = 0,
                MaxTemperature = 0,
                MinTemperature = 0,
                AverageHumidity = 0,
                AverageWindSpeed = 0,
                TotalPrecipitation = 0,
                TotalRecords = 0,
                MostCommonCondition = "No Data Available"
            };
        }

        var stats = new WeatherStatistics
        {
            AverageTemperature = Math.Round(weatherData.Average(w => w.Temperature), 2),
            MaxTemperature = Math.Round(weatherData.Max(w => w.Temperature), 2),
            MinTemperature = Math.Round(weatherData.Min(w => w.Temperature), 2),
            AverageHumidity = Math.Round(weatherData.Average(w => w.Humidity), 2),
            AverageWindSpeed = Math.Round(weatherData.Average(w => w.WindSpeed), 2),
            TotalPrecipitation = Math.Round(weatherData.Sum(w => w.Precipitation), 2),
            TotalRecords = weatherData.Count
        };

        // Find most common weather condition
        var mostCommonCondition = weatherData
            .GroupBy(w => w.WeatherConditionId)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (mostCommonCondition != null)
        {
            var condition = await _unitOfWork.WeatherConditions.GetByIdAsync(mostCommonCondition.Key);
            stats.MostCommonCondition = condition?.Description ?? "Unknown";
        }

        _logger.LogInformation("Statistics calculated: AvgTemp={AvgTemp}, MaxTemp={MaxTemp}, MinTemp={MinTemp}, Records={Records}",
            stats.AverageTemperature, stats.MaxTemperature, stats.MinTemperature, stats.TotalRecords);

        return stats;
    }
    public async Task<bool> FetchAndStoreWeatherDataAsync(int cityId)
    {
        var city = await _unitOfWork.Cities.GetByIdAsync(cityId);
        if (city == null)
            throw new Core.Exceptions.WeatherException($"City with ID {cityId} not found", 404);

        try
        {
            _logger.LogInformation("Starting to fetch weather data for city: {CityName} (ID: {CityId})", city.Name, cityId);

            var apiResponse = await _openMeteoService.GetWeatherDataAsync(city.Latitude, city.Longitude, 1);

            if (apiResponse?.Current == null)
            {
                _logger.LogWarning("API response or Current data is null for city {CityName}", city.Name);
                return false;
            }

            // Map weather code to condition
            var weatherCondition = await MapWeatherCodeToCondition(apiResponse.Current.WeatherCode ?? 0);

            // Parse timestamp safely
            DateTime timestamp;
            if (string.IsNullOrEmpty(apiResponse.Current.Time))
            {
                timestamp = DateTime.UtcNow;
            }
            else if (!DateTime.TryParse(apiResponse.Current.Time, out timestamp))
            {
                timestamp = DateTime.UtcNow;
            }

            var weatherData = new WeatherData
            {
                CityId = cityId,
                Timestamp = timestamp,
                Temperature = (decimal)apiResponse.Current.Temperature,
                FeelsLike = (decimal)apiResponse.Current.ApparentTemperature,
                Humidity = (decimal)apiResponse.Current.Humidity,
                WindSpeed = (decimal)apiResponse.Current.WindSpeed,
                WindDirection = apiResponse.Current.WindDirection ?? 0,
                Pressure = (decimal)apiResponse.Current.Pressure,
                Precipitation = (decimal)apiResponse.Current.Precipitation,
                WeatherConditionId = weatherCondition?.Id ?? 1,
                CreatedAt = DateTime.UtcNow
            };

            // Check if data already exists for today
            var existingData = (await _unitOfWork.WeatherData.FindAsync(w =>
                w.CityId == cityId && w.Timestamp.Date == weatherData.Timestamp.Date)).FirstOrDefault();

            if (existingData != null)
            {
                // Update existing entity properly
                existingData.Temperature = weatherData.Temperature;
                existingData.FeelsLike = weatherData.FeelsLike;
                existingData.Humidity = weatherData.Humidity;
                existingData.WindSpeed = weatherData.WindSpeed;
                existingData.WindDirection = weatherData.WindDirection;
                existingData.Pressure = weatherData.Pressure;
                existingData.Precipitation = weatherData.Precipitation;
                existingData.WeatherConditionId = weatherData.WeatherConditionId;
                existingData.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.WeatherData.UpdateAsync(existingData);
                _logger.LogInformation("Updated existing weather data for city {CityName}", city.Name);
            }
            else
            {
                await _unitOfWork.WeatherData.AddAsync(weatherData);
                _logger.LogInformation("Added new weather data for city {CityName}", city.Name);
            }

            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Successfully stored weather data for city {CityName}", city.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and storing weather data for city {CityName}", city.Name);
            throw;
        }
    }
    public async Task FetchAndStoreAllCitiesWeatherAsync()
    {
        var cities = await _unitOfWork.Cities.GetAllAsync();
        
        foreach (var city in cities)
        {
            if (city.IsActive)
            {
                await FetchAndStoreWeatherDataAsync(city.Id);
                await Task.Delay(1000); // Rate limiting
            }
        }
    }
    
    private async Task<WeatherCondition?> MapWeatherCodeToCondition(int weatherCode)
    {
        // WMO Weather interpretation codes (WW)
        // https://www.open-meteo.com/en/docs
        string conditionCode = weatherCode switch
        {
            0 => "clear",
            1 or 2 or 3 => "clouds",
            45 or 48 => "mist",
            51 or 53 or 55 or 56 or 57 => "rain",
            61 or 63 or 65 or 66 or 67 => "rain",
            71 or 73 or 75 or 77 => "snow",
            80 or 81 or 82 => "rain",
            85 or 86 => "snow",
            95 or 96 or 99 => "thunderstorm",
            _ => "clear"
        };
        
        var conditions = await _unitOfWork.WeatherConditions.FindAsync(c => c.Code == conditionCode);
        return conditions.FirstOrDefault();
    }
}