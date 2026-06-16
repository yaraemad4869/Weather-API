using System;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.SwaggerGen;
using WeatherProject.API.BackgroundServices;
using WeatherProject.API.Middleware;
using WeatherProject.Core.Interfaces;
using WeatherProject.Infrastructure.Data;
using WeatherProject.Infrastructure.Repositories;
using WeatherProject.Infrastructure.Services;
using WeatherProject.Infrastructure.UnitOfWork;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Async(a => a.Console())
            .WriteTo.Async(a => a.File("log.txt", rollingInterval: RollingInterval.Day))
            .WriteTo.Async(a => a.Debug())
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            // Add services to container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options => // This line is the key change
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Weather Project API v1",
                    Version = "v1",
                    Description = "A comprehensive weather data aggregation API"
                });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Configure Database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("MonsterApi"),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(5);
                        sqlOptions.CommandTimeout(30);
                    }));

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
                if (string.IsNullOrEmpty(redisConnectionString))
                {
                    redisConnectionString = builder.Configuration["Redis:Configuration:ConnectionString"];
                }
                options.Configuration = redisConnectionString;
                options.InstanceName = builder.Configuration["Redis:Configuration:ClientName"] ?? "WeatherProject_";
            });

            // Configure Authentication (optional - for production)
            if (builder.Configuration.GetValue<bool>("Auth:Enabled"))
            {
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = builder.Configuration["Auth:Issuer"],
                            ValidAudience = builder.Configuration["Auth:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Auth:Secret"]))
                        };
                    });
            }

            // Configure HTTP Client
            builder.Services.AddHttpClient<OpenMeteoService>(client =>
            {
                client.BaseAddress = new Uri("https://api.open-meteo.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "WeatherProject/1.0");
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                return Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .OrResult(msg => !msg.IsSuccessStatusCode)
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryCount, context) =>
                        {
                            serviceProvider.GetService<ILogger<OpenMeteoService>>()
                                ?.LogWarning($"Retry {retryCount} after {timespan.TotalSeconds}s");
                        });
            });

            // Register Services
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IWeatherService, WeatherService>();
            builder.Services.AddScoped<OpenMeteoService>();
            builder.Services.AddScoped<ICityService, CityService>();

            // Configure Health Checks
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>()
                .AddRedis(builder.Configuration.GetConnectionString("Redis"), "Redis Health")
                .AddUrlGroup(new Uri("https://api.open-meteo.com/v1/forecast"), "Open-Meteo API")
                .AddCheck("self", () => HealthCheckResult.Healthy("Website is healthy"))
                .AddDiskStorageHealthCheck(
                    setup: options => options.AddDrive("C:\\", 1024),
                    name: "disk_storage",
                    failureStatus: HealthStatus.Degraded);

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Configure Background Services
            builder.Services.AddHostedService<WeatherDataBackgroundService>();

            // Configure Response Caching
            builder.Services.AddResponseCaching();

            // Configure Rate Limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));
                options.AddFixedWindowLimiter("Normal", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });
                options.AddFixedWindowLimiter("Fixed", options =>
                {
                    options.PermitLimit = 100; 
                    options.Window = TimeSpan.FromMinutes(1);
                    options.QueueLimit = 0; 
                    options.AutoReplenishment = true; 
                });

                options.AddFixedWindowLimiter("Heavy", options =>
                {
                    options.PermitLimit = 20;
                    options.Window = TimeSpan.FromMinutes(5);
                    options.QueueLimit = 2;
                });

                options.AddFixedWindowLimiter("Fetch", options =>
                {
                    options.PermitLimit = 5;
                    options.Window = TimeSpan.FromMinutes(10);
                    options.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("Unlimited", options =>
                {
                    options.PermitLimit = int.MaxValue;
                    options.Window = TimeSpan.FromMinutes(1);
                    options.QueueLimit = int.MaxValue;
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
                    }

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "Too many requests",
                        message = "You have exceeded the rate limit. Please try again later.",
                        retryAfter = retryAfter.TotalSeconds,
                        timestamp = DateTime.UtcNow
                    }, cancellationToken);
                };
            });

            var app = builder.Build();

            Log.Information("Starting Weather Project API");

            var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            // Configure pipeline
            var enableSwagger = app.Configuration.GetValue<bool>("SwaggerSettings:EnableSwagger", false);
            if (app.Environment.IsDevelopment() || enableSwagger)
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather Project API v1");
                    options.RoutePrefix = string.Empty;
                });
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseRouting();
            app.UseResponseCaching();
            app.UseRateLimiter();

            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();

            if (builder.Configuration.GetValue<bool>("Auth:Enabled"))
            {
                app.UseAuthentication();
            }

            app.UseAuthorization();

            app.MapControllers();

            // Health check endpoints
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    var result = JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            description = e.Value.Description,
                            exception = e.Value.Exception?.Message
                        })
                    });

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(result);
                }
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            // Auto-migrate database
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                try
                {
                    await dbContext.Database.MigrateAsync();
                    Log.Information("Database migration completed successfully");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while migrating the database");
                }
            }

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}