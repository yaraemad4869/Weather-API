using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherProject.Core.Entities;

public class WeatherData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    public int CityId { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Temperature { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal FeelsLike { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Humidity { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal WindSpeed { get; set; }
    
    [Required]
    public int WindDirection { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Pressure { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Precipitation { get; set; }
    
    [Required]
    public int WeatherConditionId { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Properties
    [ForeignKey(nameof(CityId))]
    public virtual City? City { get; set; }
    
    [ForeignKey(nameof(WeatherConditionId))]
    public virtual WeatherCondition? WeatherCondition { get; set; }
}