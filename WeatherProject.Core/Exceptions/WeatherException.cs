namespace WeatherProject.Core.Exceptions;

public class WeatherException : Exception
{
    public int StatusCode { get; set; }
    
    public WeatherException(string message) : base(message)
    {
        StatusCode = 500;
    }
    
    public WeatherException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
    
    public WeatherException(string message, Exception innerException) : base(message, innerException)
    {
        StatusCode = 500;
    }
}