#nullable enable

namespace Weather.Models;

public record DailyForecastResponse(bool Success, string Location, List<DailyForecast> Forecasts, string? Error = null);

public record DailyForecast(
    DateOnly Date,
    string Conditions,
    double HighTemp,
    double LowTemp,
    int PrecipitationPercent,
    double WindSpeed);
