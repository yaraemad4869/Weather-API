//using System.Text.Json;
//using Microsoft.Extensions.Logging;
//using WeatherProject.Core.Interfaces.Repositories;
////using WeatherProject.Shared.DTOs;

//namespace WeatherProject.Infrastructure.Services
//{
//    public class WeatherApiService : IWeatherApiService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly ILogger<WeatherApiService> _logger;
        
//        public WeatherApiService(HttpClient httpClient, ILogger<WeatherApiService> logger)
//        {
//            _httpClient = httpClient;
//            _logger = logger;
//        }
        
//        public async Task<OpenMeteoResponse?> FetchWeatherDataAsync(double lat, double lon)
//        {
//            try
//            {
//                var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true&hourly=temperature_2m,relativehumidity_2m,windspeed_10m";
                
//                // استخدام CancellationToken للتحكم
//                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
//                var response = await _httpClient.GetAsync(url, cts.Token);
//                response.EnsureSuccessStatusCode();
                
//                var json = await response.Content.ReadAsStringAsync(cts.Token);
//                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
//                return JsonSerializer.Deserialize<OpenMeteoResponse>(json, options);
//            }
//            catch (TaskCanceledException)
//            {
//                _logger.LogWarning("Request timeout for lat={Lat}, lon={Lon}", lat, lon);
//                return null;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error fetching weather data");
//                return null;
//            }
//        }
//    }
//}