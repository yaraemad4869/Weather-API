using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WeatherProject.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherConditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Temperature = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FeelsLike = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Humidity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    WindSpeed = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    WindDirection = table.Column<int>(type: "int", nullable: false),
                    Pressure = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Precipitation = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    WeatherConditionId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherData_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeatherData_WeatherConditions_WeatherConditionId",
                        column: x => x.WeatherConditionId,
                        principalTable: "WeatherConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "Country", "CreatedAt", "IsActive", "Latitude", "Longitude", "Name", "Timezone", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Egypt", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 30.0444m, 31.2357m, "Cairo", "Africa/Cairo", null },
                    { 2, "Egypt", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 31.2001m, 29.9187m, "Alexandria", "Africa/Cairo", null },
                    { 3, "Egypt", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 29.9870m, 31.2118m, "Giza", "Africa/Cairo", null }
                });

            migrationBuilder.InsertData(
                table: "WeatherConditions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Icon" },
                values: new object[,]
                {
                    { 1, "clear", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Clear sky", "01d" },
                    { 2, "clouds", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cloudy", "04d" },
                    { 3, "rain", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rain", "10d" },
                    { 4, "snow", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Snow", "13d" },
                    { 5, "thunderstorm", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thunderstorm", "11d" },
                    { 6, "mist", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mist", "50d" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Name_Country",
                table: "Cities",
                columns: new[] { "Name", "Country" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherData_CityId_Timestamp",
                table: "WeatherData",
                columns: new[] { "CityId", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherData_Timestamp",
                table: "WeatherData",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherData_WeatherConditionId",
                table: "WeatherData",
                column: "WeatherConditionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherData");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "WeatherConditions");
        }
    }
}
