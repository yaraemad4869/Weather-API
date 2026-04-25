using System.Diagnostics;
using Serilog;

namespace WeatherProject.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDiagnosticContext _diagnosticContext;
    
    public RequestLoggingMiddleware(RequestDelegate next, IDiagnosticContext diagnosticContext)
    {
        _next = next;
        _diagnosticContext = diagnosticContext;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            _diagnosticContext.Set("RequestMethod", context.Request.Method);
            _diagnosticContext.Set("RequestPath", context.Request.Path);
            _diagnosticContext.Set("ResponseStatusCode", context.Response.StatusCode);
            _diagnosticContext.Set("ElapsedMilliseconds", stopwatch.ElapsedMilliseconds);
            _diagnosticContext.Set("RemoteIpAddress", context.Connection.RemoteIpAddress?.ToString());
            
            Log.Information("HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}