using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherProject.Core.Entities;

public class City
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(10,7)")]
    public decimal Latitude { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(10,7)")]
    public decimal Longitude { get; set; }
    
    [StringLength(50)]
    public string? Timezone { get; set; }
    
    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;


    public DateTime? UpdatedAt { get; set; }
    
    // Navigation Property
    public virtual ICollection<WeatherData>? WeatherRecords { get; set; }
}