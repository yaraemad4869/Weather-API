//using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WeatherProject.Core.Entities
{

    [Index(nameof(CityId), nameof(RecordedAt))]
    public class WeatherRecord
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string CityName { get; set; } = string.Empty;
        
        [Required]
        public DateTime RecordedAt { get; set; } // UTC time
        
        public double TemperatureCelsius { get; set; }
        public double Humidity { get; set; }
        public double WindSpeedKmh { get; set; }
        public string WeatherCondition { get; set; } = string.Empty;
        
        // العلاقات: سجل الطقس ينتمي إلى مدينة
        public int CityId { get; set; }
        
        [ForeignKey(nameof(CityId))]
        public virtual City? City { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}