using WeatherProject.Core.DTOs;

namespace WeatherProject.Core.Interfaces;

public interface ICityService
{
    Task<CityResponseDto> CreateCityAsync(CreateCityDto createCityDto);
    Task<CityResponseDto> UpdateCityAsync(int id, UpdateCityDto updateCityDto);
    Task<bool> DeleteCityAsync(int id);
    Task<CityResponseDto?> GetCityByIdAsync(int id);
    Task<IEnumerable<CityResponseDto>> GetAllCitiesAsync();
    Task<PagedResult<CityResponseDto>> SearchCitiesAsync(CitySearchDto searchDto);
    Task<bool> CityExistsAsync(string name, string country);
    Task<int> GetTotalActiveCitiesCountAsync();
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => Page * PageSize < TotalCount;
    public bool HasPreviousPage => Page > 1;
}