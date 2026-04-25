using System.ComponentModel.DataAnnotations;

namespace WeatherProject.Core.DTOs;

public class CreateCityDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Country { get; set; } = string.Empty;
    
    [Required]
    [Range(-90, 90)]
    public decimal Latitude { get; set; }
    
    [Required]
    [Range(-180, 180)]
    public decimal Longitude { get; set; }
    
    [StringLength(50)]
    public string? Timezone { get; set; }
    
    public bool IsActive { get; set; } = true;
}

public class UpdateCityDto
{
    [StringLength(100)]
    public string? Name { get; set; }
    
    [StringLength(100)]
    public string? Country { get; set; }
    
    [Range(-90, 90)]
    public decimal? Latitude { get; set; }
    
    [Range(-180, 180)]
    public decimal? Longitude { get; set; }
    
    [StringLength(50)]
    public string? Timezone { get; set; }
    
    public bool? IsActive { get; set; }
}

public class CityResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Timezone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int WeatherRecordsCount { get; set; }
}

public class CitySearchDto
{
    public string? SearchTerm { get; set; }
    public string? Country { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}