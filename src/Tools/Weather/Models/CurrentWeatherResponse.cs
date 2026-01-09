#nullable enable

namespace Weather.Models;

public record CurrentWeatherResponse(bool Success, CurrentWeather? Weather, string? Error = null);

public record CurrentWeather(
    string Location,
    string Conditions,
    double Temperature,
    double FeelsLike,
    int Humidity,
    double WindSpeed,
    int WindDirection,
    int UvIndex);
