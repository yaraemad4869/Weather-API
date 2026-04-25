using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using WeatherProject.Core.Interfaces;
using WeatherProject.Core.Entities;

namespace WeatherProject.Infrastructure.Services;

public class OpenMeteoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenMeteoService> _logger;
    private readonly IDistributedCache _cache;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    
    public OpenMeteoService(HttpClient httpClient, ILogger<OpenMeteoService> logger, IDistributedCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        
        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning($"Retry {retryCount} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });
    }
    
    public async Task<OpenMeteoResponse?> GetWeatherDataAsync(decimal latitude, decimal longitude, int days = 1)
    {
        var cacheKey = $"weather_{latitude}_{longitude}_{days}_{DateTime.UtcNow:yyyyMMddHH}";
        
        // Try to get from cache
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (cachedData != null)
        {
            _logger.LogInformation("Returning cached weather data for {Latitude}, {Longitude}", latitude, longitude);
            return JsonSerializer.Deserialize<OpenMeteoResponse>(cachedData);
        }
        
        try
        {
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,rain,showers,weather_code,wind_speed_10m,wind_direction_10m,pressure_msl&hourly=temperature_2m,relative_humidity_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum&timezone=auto&forecast_days={days}";
            
            var response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(url));
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<OpenMeteoResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            // Cache for 1 hour
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };
            await _cache.SetStringAsync(cacheKey, json, cacheOptions);
            
            return weatherData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather data from Open-Meteo API");
            throw;
        }
    }
}

public class OpenMeteoResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("current")]
    public OpenMeteoCurrent? Current { get; set; }

    [JsonPropertyName("hourly")]
    public OpenMeteoHourly? Hourly { get; set; }

    [JsonPropertyName("daily")]
    public OpenMeteoDaily? Daily { get; set; }
}

public class OpenMeteoCurrent
{
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("temperature_2m")]
    public double? Temperature { get; set; }

    [JsonPropertyName("apparent_temperature")]
    public double? ApparentTemperature { get; set; }

    [JsonPropertyName("relative_humidity_2m")]
    public double? Humidity { get; set; }

    [JsonPropertyName("precipitation")]
    public double? Precipitation { get; set; }

    [JsonPropertyName("rain")]
    public double? Rain { get; set; }

    [JsonPropertyName("showers")]
    public double? Showers { get; set; }

    [JsonPropertyName("weather_code")]
    public int? WeatherCode { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public double? WindSpeed { get; set; }

    [JsonPropertyName("wind_direction_10m")]
    public int? WindDirection { get; set; }

    [JsonPropertyName("pressure_msl")]
    public double? Pressure { get; set; }
}

public class OpenMeteoHourly
{
    public List<string>? Time { get; set; }
    [JsonPropertyName("temperature_2m")]
    public List<decimal>? Temperature { get; set; }
    [JsonPropertyName("relative_humidity_2m")]
    public List<decimal>? Humidity { get; set; }
}

public class OpenMeteoDaily
{
    public List<string>? Time { get; set; }
    [JsonPropertyName("temperature_2m_max")]
    public List<decimal>? TemperatureMax { get; set; }
    [JsonPropertyName("temperature_2m_min")]
    public List<decimal>? TemperatureMin { get; set; }
}