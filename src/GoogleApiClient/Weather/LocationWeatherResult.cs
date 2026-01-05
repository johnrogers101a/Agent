namespace GoogleApiClient.Weather;

/// <summary>
/// Weather result with location context for tool responses.
/// </summary>
public record LocationWeatherResult(
    string Location,
    WeatherResult Weather);
