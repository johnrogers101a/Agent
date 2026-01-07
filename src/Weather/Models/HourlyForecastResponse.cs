#nullable enable

namespace Weather.Models;

public record HourlyForecastResponse(bool Success, string Location, List<HourlyForecast> Forecasts, string? Error = null);

public record HourlyForecast(
    DateTime Time,
    string Conditions,
    double Temperature,
    int PrecipitationPercent);
