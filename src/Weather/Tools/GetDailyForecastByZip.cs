#nullable enable

using System.Net.Http.Json;
using System.Text.Json;
using AgentFramework.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Weather.Models;

namespace Weather.Tools;

/// <summary>
/// Gets daily weather forecast for a US zip code using Google Weather API.
/// </summary>
public class GetDailyForecastByZip
{
    private readonly HttpClient _http;
    private readonly ILogger<GetDailyForecastByZip> _logger;
    private readonly string _apiKey;

    public GetDailyForecastByZip(HttpClient httpClient, IConfiguration config, ILogger<GetDailyForecastByZip> logger)
    {
        _http = httpClient;
        _logger = logger;
        _apiKey = config[ConfigKeys.ApiKey] ?? throw new InvalidOperationException(Errors.ApiKeyNotConfigured);
    }

    /// <summary>
    /// Gets daily weather forecast for a US zip code.
    /// </summary>
    /// <param name="zipCode">US zip code (e.g., 98052).</param>
    /// <param name="days">Number of days (1-10).</param>
    [McpTool]
    public async Task<DailyForecastResponse> ExecuteAsync(string zipCode, int days)
    {
        days = Math.Clamp(days, 1, 10);
        _logger.LogTrace("GetDailyForecastByZip starting for ZipCode={ZipCode}, Days={Days}", zipCode, days);

        if (string.IsNullOrWhiteSpace(zipCode))
        {
            _logger.LogWarning("GetDailyForecastByZip failed: ZipCode is required");
            return new DailyForecastResponse(false, "", [], Errors.ZipCodeRequired);
        }

        try
        {
            // Geocode zip
            var geoUrl = $"{Urls.GoogleGeocode}?address={zipCode}&key={_apiKey}";
            _logger.LogTrace("GetDailyForecastByZip geocoding via {Url}", geoUrl);

            var geo = await _http.GetFromJsonAsync<JsonElement>(geoUrl);
            _logger.LogTrace("GetDailyForecastByZip received geocode response");

            if (!geo.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                _logger.LogWarning("GetDailyForecastByZip could not geocode {ZipCode}", zipCode);
                return new DailyForecastResponse(false, "", [], string.Format(Errors.LocationNotFoundZip, zipCode));
            }

            var loc = results[0].GetProperty("geometry").GetProperty("location");
            var lat = loc.GetProperty("lat").GetDouble();
            var lng = loc.GetProperty("lng").GetDouble();
            _logger.LogTrace("GetDailyForecastByZip geocoded to Lat={Lat}, Lng={Lng}", lat, lng);

            // Get forecast
            var forecastUrl = $"{Urls.GoogleWeatherDaily}?location.latitude={lat}&location.longitude={lng}&days={days}&key={_apiKey}";
            _logger.LogTrace("GetDailyForecastByZip fetching forecast from {Url}", forecastUrl);

            var forecast = await _http.GetFromJsonAsync<JsonElement>(forecastUrl);
            _logger.LogTrace("GetDailyForecastByZip received forecast response");

            var forecasts = new List<DailyForecast>();
            foreach (var day in forecast.GetProperty("forecastDays").EnumerateArray())
            {
                var d = day.GetProperty("displayDate");
                var date = new DateOnly(d.GetProperty("year").GetInt32(), d.GetProperty("month").GetInt32(), d.GetProperty("day").GetInt32());
                var daytime = day.GetProperty("daytimeForecast");

                forecasts.Add(new DailyForecast(
                    date,
                    daytime.GetProperty("weatherCondition").GetProperty("description").GetProperty("text").GetString() ?? "",
                    day.GetProperty("maxTemperature").GetProperty("degrees").GetDouble(),
                    day.GetProperty("minTemperature").GetProperty("degrees").GetDouble(),
                    daytime.GetProperty("precipitation").GetProperty("probability").GetProperty("percent").GetInt32(),
                    daytime.GetProperty("wind").GetProperty("speed").GetProperty("value").GetDouble()));

                _logger.LogTrace("GetDailyForecastByZip parsed forecast for {Date}", date);
            }

            _logger.LogTrace("GetDailyForecastByZip completed with {Count} days", forecasts.Count);
            return new DailyForecastResponse(true, zipCode, forecasts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetDailyForecastByZip failed with exception");
            return new DailyForecastResponse(false, "", [], string.Format(Errors.GenericError, ex.Message));
        }
    }
}
