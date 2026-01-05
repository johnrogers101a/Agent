namespace GoogleApiClient.Models;

public record WeatherToolResult(
    string Location,
    string Description,
    double Temperature,
    double FeelsLike,
    string TemperatureUnit,
    int Humidity,
    double WindSpeed,
    string WindDirection,
    int UvIndex);
