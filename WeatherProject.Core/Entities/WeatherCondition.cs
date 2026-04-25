using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherProject.Core.Entities;

public class WeatherCondition
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string? Icon { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public virtual ICollection<WeatherData>? WeatherRecords { get; set; }
}