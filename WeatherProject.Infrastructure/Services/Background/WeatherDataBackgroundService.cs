using WeatherProject.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace WeatherProject.API.BackgroundServices;

public class WeatherDataBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeatherDataBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Fetch every hour
    
    public WeatherDataBackgroundService(IServiceProvider serviceProvider, ILogger<WeatherDataBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Weather Data Background Service is starting");
        
        // Wait 5 minutes before first execution to allow app to initialize
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting scheduled weather data fetch");
                
                using var scope = _serviceProvider.CreateScope();
                var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();
                
                await weatherService.FetchAndStoreAllCitiesWeatherAsync();
                
                _logger.LogInformation("Completed scheduled weather data fetch");
                
                await Task.Delay(_interval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during background weather data fetch");
                
                // Wait 5 minutes before retry on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        
        _logger.LogInformation("Weather Data Background Service is stopping");
    }
}