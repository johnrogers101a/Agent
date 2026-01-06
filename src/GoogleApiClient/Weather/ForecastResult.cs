namespace GoogleApiClient.Weather;

/// <summary>
/// Daily forecast result for a single day.
/// </summary>
public record ForecastDayResult(
    DateOnly Date,
    string Condition,
    double HighTemp,
    double LowTemp,
    string TempUnit,
    int PrecipChance,
    int Humidity,
    double WindSpeed,
    string WindDirection,
    int UvIndex
);

/// <summary>
/// Hourly forecast result for a single hour.
/// </summary>
public record ForecastHourResult(
    DateTime DateTime,
    string Condition,
    double Temp,
    double FeelsLike,
    string TempUnit,
    int PrecipChance,
    int Humidity,
    double WindSpeed,
    string WindDirection,
    int UvIndex
);

/// <summary>
/// Daily forecast result with location context for tool responses.
/// </summary>
public record LocationDailyForecastResult(
    string Location,
    List<ForecastDayResult> Forecast
);

/// <summary>
/// Hourly forecast result with location context for tool responses.
/// </summary>
public record LocationHourlyForecastResult(
    string Location,
    List<ForecastHourResult> Forecast
);
