//using AutoMapper;

//public class WeatherProfile : Profile
//{
//    public WeatherProfile()
//    {
//        CreateMap<OpenMeteoResponse, WeatherRecord>()
//            .ForMember(dest => dest.TemperatureCelsius, opt => opt.MapFrom(src => src.CurrentWeather.Temperature))
//            .ForMember(dest => dest.WindSpeedKmh, opt => opt.MapFrom(src => src.CurrentWeather.Windspeed))
//            .ForMember(dest => dest.RecordedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
//    }
//}