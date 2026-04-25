//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using WeatherProject.Core.Entities;

//public class WeatherCollectionBackgroundService : BackgroundService
//{
//    private readonly IServiceProvider _services;
//    private readonly ILogger<WeatherCollectionBackgroundService> _logger;
//    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1)); // كل ساعة
    
//    public WeatherCollectionBackgroundService(IServiceProvider services, ILogger<WeatherCollectionBackgroundService> logger)
//    {
//        _services = services;
//        _logger = logger;
//    }
    
//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
//        {
//            await CollectWeatherDataForAllCities(stoppingToken);
//        }
//    }
    
//    private async Task CollectWeatherDataForAllCities(CancellationToken ct)
//    {
//        using var scope = _services.CreateScope();
//        var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherApiService>();
//        var cityRepo = scope.ServiceProvider.GetRequiredService<IWeatherRepository<City>>();
//        var weatherRepo = scope.ServiceProvider.GetRequiredService<IWeatherRepository<WeatherRecord>>();
        
//        var activeCities = await cityRepo.GetAllAsync();
        
//        foreach (var city in activeCities.Where(c => c.IsActive))
//        {
//            try
//            {
//                var apiData = await weatherService.FetchWeatherDataAsync(city.Latitude, city.Longitude);
                
//                if (apiData?.CurrentWeather != null)
//                {
//                    var record = new WeatherRecord
//                    {
//                        CityId = city.Id,
//                        CityName = city.Name,
//                        RecordedAt = DateTime.UtcNow,
//                        TemperatureCelsius = apiData.CurrentWeather.Temperature,
//                        Humidity = apiData.Hourly.RelativeHumidity?.FirstOrDefault() ?? 0,
//                        WindSpeedKmh = apiData.CurrentWeather.Windspeed,
//                        WeatherCondition = "Clear" // يمكن تحسينها
//                    };
                    
//                    await weatherRepo.AddAsync(record, ct);
//                    await weatherRepo.SaveChangesAsync(ct);
//                    _logger.LogInformation("Saved weather for {City}", city.Name);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to fetch for {City}", city.Name);
//            }
//        }
//    }
//}