using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WeatherProject.Core.DTOs;
using WeatherProject.Core.Entities;
using WeatherProject.Core.Exceptions;
using WeatherProject.Core.Interfaces;

namespace WeatherProject.Infrastructure.Services;

public class CityService : ICityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CityService> _logger;
    
    public CityService(IUnitOfWork unitOfWork, ILogger<CityService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<CityResponseDto> CreateCityAsync(CreateCityDto createCityDto)
    {
        try
        {
            // Check if city already exists
            var exists = await CityExistsAsync(createCityDto.Name, createCityDto.Country);
            if (exists)
            {
                throw new WeatherException($"City {createCityDto.Name}, {createCityDto.Country} already exists", 409);
            }
            
            // Auto-detect timezone if not provided
            string? timezone = createCityDto.Timezone;
            if (string.IsNullOrEmpty(timezone))
            {
                timezone = await GetTimezoneFromCoordinates(createCityDto.Latitude, createCityDto.Longitude);
            }
            
            var city = new City
            {
                Name = createCityDto.Name,
                Country = createCityDto.Country,
                Latitude = createCityDto.Latitude,
                Longitude = createCityDto.Longitude,
                Timezone = timezone,
                IsActive = createCityDto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Cities.AddAsync(city);
            await _unitOfWork.CompleteAsync();
            
            _logger.LogInformation("Created new city: {Name}, {Country}", city.Name, city.Country);
            
            return await MapToResponseDto(city);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating city: {Name}, {Country}", createCityDto.Name, createCityDto.Country);
            throw;
        }
    }
    
    public async Task<CityResponseDto> UpdateCityAsync(int id, UpdateCityDto updateCityDto)
    {
        var city = await _unitOfWork.Cities.GetByIdAsync(id);
        if (city == null)
        {
            throw new WeatherException($"City with ID {id} not found", 404);
        }
        
        if (!string.IsNullOrEmpty(updateCityDto.Name))
            city.Name = updateCityDto.Name;
            
        if (!string.IsNullOrEmpty(updateCityDto.Country))
            city.Country = updateCityDto.Country;
            
        if (updateCityDto.Latitude.HasValue)
            city.Latitude = updateCityDto.Latitude.Value;
            
        if (updateCityDto.Longitude.HasValue)
            city.Longitude = updateCityDto.Longitude.Value;
            
        if (!string.IsNullOrEmpty(updateCityDto.Timezone))
            city.Timezone = updateCityDto.Timezone;
            
        if (updateCityDto.IsActive.HasValue)
            city.IsActive = updateCityDto.IsActive.Value;
            
        city.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.Cities.UpdateAsync(city);
        await _unitOfWork.CompleteAsync();
        
        _logger.LogInformation("Updated city: {Name}, {Country}", city.Name, city.Country);
        
        return await MapToResponseDto(city);
    }
    
    public async Task<bool> DeleteCityAsync(int id)
    {
        var city = await _unitOfWork.Cities.GetByIdAsync(id);
        if (city == null)
        {
            throw new WeatherException($"City with ID {id} not found", 404);
        }
        
        // Check if city has weather data
        var weatherRecords = await _unitOfWork.WeatherData.FindAsync(w => w.CityId == id);
        if (weatherRecords.Any())
        {
            throw new WeatherException($"Cannot delete city with existing weather data. Consider deactivating it instead.", 400);
        }
        
        await _unitOfWork.Cities.DeleteAsync(id);
        await _unitOfWork.CompleteAsync();
        
        _logger.LogInformation("Deleted city: {Name}, {Country}", city.Name, city.Country);
        
        return true;
    }
    
    public async Task<CityResponseDto?> GetCityByIdAsync(int id)
    {
        var city = await _unitOfWork.Cities.GetByIdAsync(id);
        if (city == null)
            return null;
            
        return await MapToResponseDto(city);
    }
    
    public async Task<IEnumerable<CityResponseDto>> GetAllCitiesAsync()
    {
        var cities = await _unitOfWork.Cities.GetAllAsync();
        var result = new List<CityResponseDto>();
        
        foreach (var city in cities)
        {
            result.Add(await MapToResponseDto(city));
        }
        
        return result;
    }
    
    public async Task<PagedResult<CityResponseDto>> SearchCitiesAsync(CitySearchDto searchDto)
    {
        var query = _unitOfWork.Cities.FindAsync(c => true).Result.AsQueryable();
        
        // Apply filters
        if (!string.IsNullOrEmpty(searchDto.SearchTerm))
        {
            query = query.Where(c => 
                c.Name.Contains(searchDto.SearchTerm) || 
                c.Country.Contains(searchDto.SearchTerm));
        }
        
        if (!string.IsNullOrEmpty(searchDto.Country))
        {
            query = query.Where(c => c.Country == searchDto.Country);
        }
        
        if (searchDto.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == searchDto.IsActive.Value);
        }
        
        var totalCount = query.Count();
        
        var cities = query
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToList();
        
        var items = new List<CityResponseDto>();
        foreach (var city in cities)
        {
            items.Add(await MapToResponseDto(city));
        }
        
        return new PagedResult<CityResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize
        };
    }
    
    public async Task<bool> CityExistsAsync(string name, string country)
    {
        var cities = await _unitOfWork.Cities.FindAsync(c => 
            c.Name == name && c.Country == country);
        return cities.Any();
    }
    
    public async Task<int> GetTotalActiveCitiesCountAsync()
    {
        return await _unitOfWork.Cities.CountAsync(c => c.IsActive);
    }
    
    private async Task<string?> GetTimezoneFromCoordinates(decimal latitude, decimal longitude)
    {
        try
        {
            using var httpClient = new HttpClient();
            var url = $"https://api.timezonedb.com/v2.1/get-time-zone?key=YOUR_API_KEY&format=json&by=position&lat={latitude}&lng={longitude}";
            
            // Alternative free API
            url = $"https://api.geonames.org/timezoneJSON?lat={latitude}&lng={longitude}&username=demo";
            
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                // Parse timezone from response
                // For simplicity, return null if not available
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not auto-detect timezone for coordinates {Lat}, {Lng}", latitude, longitude);
        }
        
        return null;
    }
    
    private async Task<CityResponseDto> MapToResponseDto(City city)
    {
        var weatherCount = await _unitOfWork.WeatherData.CountAsync(w => w.CityId == city.Id);
        
        return new CityResponseDto
        {
            Id = city.Id,
            Name = city.Name,
            Country = city.Country,
            Latitude = city.Latitude,
            Longitude = city.Longitude,
            Timezone = city.Timezone,
            IsActive = city.IsActive,
            CreatedAt = city.CreatedAt??DateTime.UtcNow,
            WeatherRecordsCount = weatherCount
        };
    }
}