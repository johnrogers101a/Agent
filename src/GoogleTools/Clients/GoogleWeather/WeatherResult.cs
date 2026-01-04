namespace GoogleTools.Clients.GoogleWeather;

public record WeatherResult(
    string Description,
    double Temperature,
    double FeelsLike,
    string TemperatureUnit,
    int Humidity,
    double WindSpeed,
    string WindDirection,
    int UvIndex,
    string IconUrl,
    DateTime RetrievedAt
);
