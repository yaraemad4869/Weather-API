using System.Text.Json.Serialization;

namespace WeatherProject.Core.DTOs;

public class WeatherRequestDto
{
    public int CityId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class WeatherResponseDto
{
    public int Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal Temperature { get; set; }
    public decimal FeelsLike { get; set; }
    public decimal Humidity { get; set; }
    public decimal WindSpeed { get; set; }
    public int WindDirection { get; set; }
    public decimal Pressure { get; set; }
    public decimal Precipitation { get; set; }
    public string WeatherCondition { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Timezone { get; set; }
}

public class WeatherStatisticsDto
{
    public string CityName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AverageTemperature { get; set; }
    public decimal MaxTemperature { get; set; }
    public decimal MinTemperature { get; set; }
    public decimal AverageHumidity { get; set; }
    public decimal AverageWindSpeed { get; set; }
    public decimal TotalPrecipitation { get; set; }
    public string? MostCommonCondition { get; set; }
    public int TotalRecords { get; set; }
}