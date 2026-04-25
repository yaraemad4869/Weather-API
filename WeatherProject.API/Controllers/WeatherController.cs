using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeatherProject.Core.DTOs;
using WeatherProject.Core.Interfaces;
using WeatherProject.Core.Entities;

namespace WeatherProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("Fixed")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WeatherController> _logger;
    
    public WeatherController(IWeatherService weatherService, IUnitOfWork unitOfWork, ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    /// <summary>
    /// Get current weather for a city
    /// </summary>
    [HttpGet("current/{cityId}")]
    [ProducesResponseType(typeof(WeatherResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // Cache for 5 minutes
    public async Task<ActionResult<WeatherResponseDto>> GetCurrentWeather(int cityId)
    {
        var weather = await _weatherService.GetCurrentWeatherAsync(cityId);
        if (weather == null)
            return NotFound($"No weather data found for city ID {cityId}");
        
        return Ok(MapToResponseDto(weather));
    }

    /// <summary>
    /// Get weather forecast for a city
    /// </summary>
    [HttpGet("forecast/{cityId}")]
    [ProducesResponseType(typeof(IEnumerable<WeatherResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WeatherResponseDto>>> GetForecast(int cityId, int days = 5)
    {
        if (days < 1 || days > 10)
            return BadRequest("Days must be between 1 and 10");
        
        var forecasts = await _weatherService.GetWeatherForecastAsync(cityId, days);
        if (!forecasts.Any())
            return NotFound($"No forecast data found for city ID {cityId}");
        
        return Ok(forecasts.Select(MapToResponseDto));
    }
    
    /// <summary>
    /// Get historical weather data
    /// </summary>
    [HttpGet("historical/{cityId}")]
    [ProducesResponseType(typeof(IEnumerable<WeatherResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)] // Cache for 1 hour
    public async Task<ActionResult<IEnumerable<WeatherResponseDto>>> GetHistoricalWeather(
        int cityId, 
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest("Start date must be before end date");
        
        if ((endDate - startDate).Days > 365)
            return BadRequest("Date range cannot exceed 365 days");
        
        var historical = await _weatherService.GetHistoricalWeatherAsync(cityId, startDate, endDate);
        return Ok(historical.Select(MapToResponseDto));
    }
    
    /// <summary>
    /// Get weather statistics for a city
    /// </summary>
    [HttpGet("statistics/{cityId}")]
    [ProducesResponseType(typeof(WeatherStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<WeatherStatisticsDto>> GetWeatherStatistics(
        int cityId, 
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest("Start date must be before end date");
        
        var city = await _unitOfWork.Cities.GetByIdAsync(cityId);
        if (city == null)
            return NotFound($"City with ID {cityId} not found");
        
        var stats = await _weatherService.GetWeatherStatisticsAsync(cityId, startDate, endDate);
        
        return Ok(new WeatherStatisticsDto
        {
            CityName = city.Name,
            StartDate = startDate,
            EndDate = endDate,
            AverageTemperature = stats.AverageTemperature,
            MaxTemperature = stats.MaxTemperature,
            MinTemperature = stats.MinTemperature,
            AverageHumidity = stats.AverageHumidity,
            AverageWindSpeed = stats.AverageWindSpeed,
            TotalPrecipitation = stats.TotalPrecipitation,
            MostCommonCondition = stats.MostCommonCondition,
            TotalRecords = stats.TotalRecords
        });
    }
    
    /// <summary>
    /// Manually trigger weather data fetch
    /// </summary>
    [HttpPost("fetch/{cityId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> FetchWeatherData(int cityId)
    {
        var success = await _weatherService.FetchAndStoreWeatherDataAsync(cityId);
        if (!success)
            return BadRequest("Failed to fetch weather data");
        
        _logger.LogInformation("Manually triggered weather data fetch for city ID {CityId}", cityId);
        return Ok(new { message = "Weather data fetched successfully", cityId });
    }
    [HttpGet("check-data/{cityId}")]
    public async Task<ActionResult> CheckData(int cityId)
    {
        var city = await _unitOfWork.Cities.GetByIdAsync(cityId);
        if (city == null)
            return NotFound(new { message = $"City with ID {cityId} not found" });

        var allData = await _unitOfWork.WeatherData.FindAsync(w => w.CityId == cityId);
        var last30Days = await _unitOfWork.WeatherData.FindAsync(w =>
            w.CityId == cityId && w.Timestamp >= DateTime.UtcNow.AddDays(-30));

        return Ok(new
        {
            city = new { city.Id, city.Name, city.Country },
            totalRecords = allData.Count(),
            last30DaysRecords = last30Days.Count(),
            latestRecord = allData.OrderByDescending(w => w.Timestamp).FirstOrDefault(),
            dateRange = new
            {
                oldest = allData.Min(w => (DateTime?)w.Timestamp),
                newest = allData.Max(w => (DateTime?)w.Timestamp)
            },
            sampleData = allData.Take(5).Select(w => new
            {
                w.Timestamp,
                w.Temperature,
                w.Humidity,
                w.Precipitation
            })
        });
    }
    /// <summary>
    /// Get all cities
    /// </summary>
    [HttpGet("cities")]
    [ProducesResponseType(typeof(IEnumerable<CityDto>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)] // Cache for 24 hours
    public async Task<ActionResult<IEnumerable<CityDto>>> GetCities()
    {
        var cities = await _unitOfWork.Cities.GetAllAsync();
        return Ok(cities.Select(c => new CityDto
        {
            Id = c.Id,
            Name = c.Name,
            Country = c.Country,
            Latitude = c.Latitude,
            Longitude = c.Longitude,
            Timezone = c.Timezone
        }));
    }
    
    private WeatherResponseDto MapToResponseDto(WeatherData weather)
    {
        return new WeatherResponseDto
        {
            Id = weather.Id,
            CityName = weather.City?.Name ?? string.Empty,
            Country = weather.City?.Country ?? string.Empty,
            Timestamp = weather.Timestamp,
            Temperature = weather.Temperature,
            FeelsLike = weather.FeelsLike,
            Humidity = weather.Humidity,
            WindSpeed = weather.WindSpeed,
            WindDirection = weather.WindDirection,
            Pressure = weather.Pressure,
            Precipitation = weather.Precipitation,
            WeatherCondition = weather.WeatherCondition?.Description ?? string.Empty,
            Description = weather.Description
        };
    }
}