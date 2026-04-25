using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeatherProject.Core.DTOs;
using WeatherProject.Core.Exceptions;
using WeatherProject.Core.Interfaces;

namespace WeatherProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("Normal")]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;
    private readonly ILogger<CitiesController> _logger;
    
    public CitiesController(ICityService cityService, ILogger<CitiesController> logger)
    {
        _cityService = cityService;
        _logger = logger;
    }
    
    /// <summary>
    /// Create a new city
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CityResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CityResponseDto>> CreateCity([FromBody] CreateCityDto createCityDto)
    {
        try
        {
            var city = await _cityService.CreateCityAsync(createCityDto);
            return CreatedAtAction(nameof(GetCityById), new { id = city.Id }, city);
        }
        catch (WeatherException ex) when (ex.StatusCode == 409)
        {
            return Conflict(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Update an existing city
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CityResponseDto>> UpdateCity(int id, [FromBody] UpdateCityDto updateCityDto)
    {
        try
        {
            var city = await _cityService.UpdateCityAsync(id, updateCityDto);
            return Ok(city);
        }
        catch (WeatherException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Delete a city (only if no weather data exists)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCity(int id)
    {
        try
        {
            await _cityService.DeleteCityAsync(id);
            return NoContent();
        }
        catch (WeatherException ex) when (ex.StatusCode == 404)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (WeatherException ex) when (ex.StatusCode == 400)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Get city by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CityResponseDto>> GetCityById(int id)
    {
        var city = await _cityService.GetCityByIdAsync(id);
        if (city == null)
            return NotFound(new { message = $"City with ID {id} not found" });
        
        return Ok(city);
    }
    
    /// <summary>
    /// Get all cities
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CityResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CityResponseDto>>> GetAllCities()
    {
        var cities = await _cityService.GetAllCitiesAsync();
        return Ok(cities);
    }
    
    /// <summary>
    /// Search cities with pagination and filtering
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<CityResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CityResponseDto>>> SearchCities(
        [FromQuery] string? searchTerm,
        [FromQuery] string? country,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var searchDto = new CitySearchDto
        {
            SearchTerm = searchTerm,
            Country = country,
            IsActive = isActive,
            Page = page,
            PageSize = Math.Min(pageSize, 50) // Limit to 50 max
        };
        
        var result = await _cityService.SearchCitiesAsync(searchDto);
        return Ok(result);
    }
    
    /// <summary>
    /// Get unique countries list
    /// </summary>
    [HttpGet("countries")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetCountries()
    {
        var cities = await _cityService.GetAllCitiesAsync();
        var countries = cities.Select(c => c.Country).Distinct().OrderBy(c => c);
        return Ok(countries);
    }
    
    /// <summary>
    /// Bulk import cities from a list
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkImportResult>> BulkImportCities([FromBody] List<CreateCityDto> cities)
    {
        if (cities == null || !cities.Any())
            return BadRequest(new { message = "No cities provided" });
        
        if (cities.Count > 100)
            return BadRequest(new { message = "Maximum 100 cities per bulk import" });
        
        var result = new BulkImportResult
        {
            Total = cities.Count,
            Successful = 0,
            Failed = 0,
            Errors = new List<string>()
        };
        
        foreach (var cityDto in cities)
        {
            try
            {
                await _cityService.CreateCityAsync(cityDto);
                result.Successful++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"Failed to add {cityDto.Name}, {cityDto.Country}: {ex.Message}");
            }
        }
        
        return Ok(result);
    }
    
    /// <summary>
    /// Get weather statistics for all active cities
    /// </summary>
    [HttpGet("statistics/summary")]
    [ProducesResponseType(typeof(CitiesSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CitiesSummaryDto>> GetCitiesSummary()
    {
        var cities = await _cityService.GetAllCitiesAsync();
        var activeCities = cities.Where(c => c.IsActive).ToList();
        var totalWeatherRecords = activeCities.Sum(c => c.WeatherRecordsCount);
        
        var summary = new CitiesSummaryDto
        {
            TotalCities = cities.Count(),
            ActiveCities = activeCities.Count,
            InactiveCities = cities.Count() - activeCities.Count,
            TotalWeatherRecords = totalWeatherRecords,
            AverageRecordsPerCity = activeCities.Any() ? totalWeatherRecords / activeCities.Count : 0,
            CountriesCount = cities.Select(c => c.Country).Distinct().Count()
        };
        
        return Ok(summary);
    }
}

public class BulkImportResult
{
    public int Total { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class CitiesSummaryDto
{
    public int TotalCities { get; set; }
    public int ActiveCities { get; set; }
    public int InactiveCities { get; set; }
    public int TotalWeatherRecords { get; set; }
    public int AverageRecordsPerCity { get; set; }
    public int CountriesCount { get; set; }
}