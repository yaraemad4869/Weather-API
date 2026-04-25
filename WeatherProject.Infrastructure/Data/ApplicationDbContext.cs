using Microsoft.EntityFrameworkCore;
using WeatherProject.Core.Entities;

namespace WeatherProject.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<City> Cities { get; set; }
    public DbSet<WeatherData> WeatherData { get; set; }
    public DbSet<WeatherCondition> WeatherConditions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite index for performance
        modelBuilder.Entity<WeatherData>()
            .HasIndex(w => new { w.CityId, w.Timestamp })
            .IsUnique();

        // Configure indexes
        modelBuilder.Entity<WeatherData>()
            .HasIndex(w => w.Timestamp);

        modelBuilder.Entity<City>()
            .HasIndex(c => new { c.Name, c.Country })
            .IsUnique();

        // Configure relationships
        modelBuilder.Entity<WeatherData>()
            .HasOne(w => w.City)
            .WithMany(c => c.WeatherRecords)
            .HasForeignKey(w => w.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WeatherData>()
            .HasOne(w => w.WeatherCondition)
            .WithMany(wc => wc.WeatherRecords)
            .HasForeignKey(w => w.WeatherConditionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Weather Conditions
        var conditions = new[]
        {
            new WeatherCondition { Id = 1, Code = "clear", Description = "Clear sky", Icon = "01d", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new WeatherCondition { Id = 2, Code = "clouds", Description = "Cloudy", Icon = "04d", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new WeatherCondition { Id = 3, Code = "rain", Description = "Rain", Icon = "10d", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new WeatherCondition { Id = 4, Code = "snow", Description = "Snow", Icon = "13d", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new WeatherCondition { Id = 5, Code = "thunderstorm", Description = "Thunderstorm", Icon = "11d", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new WeatherCondition { Id = 6, Code = "mist", Description = "Mist", Icon = "50d", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };

        modelBuilder.Entity<WeatherCondition>().HasData(conditions);

        // Seed Cities
        var cities = new[]
        {
            new City { Id = 1, Name = "Cairo", Country = "Egypt", Latitude = 30.0444m, Longitude = 31.2357m, Timezone = "Africa/Cairo", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new City { Id = 2, Name = "Alexandria", Country = "Egypt", Latitude = 31.2001m, Longitude = 29.9187m, Timezone = "Africa/Cairo", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new City { Id = 3, Name = "Giza", Country = "Egypt", Latitude = 29.9870m, Longitude = 31.2118m, Timezone = "Africa/Cairo", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };

        modelBuilder.Entity<City>().HasData(cities);
    }
}