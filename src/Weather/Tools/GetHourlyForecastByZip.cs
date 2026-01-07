#nullable enable

using System.Net.Http.Json;
using System.Text.Json;
using AgentFramework.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Weather.Models;

namespace Weather.Tools;

/// <summary>
/// Gets hourly weather forecast for a US zip code using Google Weather API.
/// </summary>
public class GetHourlyForecastByZip
{
    private readonly HttpClient _http;
    private readonly ILogger<GetHourlyForecastByZip> _logger;
    private readonly string _apiKey;

    public GetHourlyForecastByZip(HttpClient httpClient, IConfiguration config, ILogger<GetHourlyForecastByZip> logger)
    {
        _http = httpClient;
        _logger = logger;
        _apiKey = config[ConfigKeys.ApiKey] ?? throw new InvalidOperationException(Errors.ApiKeyNotConfigured);
    }

    /// <summary>
    /// Gets hourly weather forecast for a US zip code.
    /// </summary>
    /// <param name="zipCode">US zip code (e.g., 98052).</param>
    /// <param name="hours">Number of hours (1-24).</param>
    [McpTool]
    public async Task<HourlyForecastResponse> ExecuteAsync(string zipCode, int hours)
    {
        hours = Math.Clamp(hours, 1, 24);
        _logger.LogTrace("GetHourlyForecastByZip starting for ZipCode={ZipCode}, Hours={Hours}", zipCode, hours);

        if (string.IsNullOrWhiteSpace(zipCode))
        {
            _logger.LogWarning("GetHourlyForecastByZip failed: ZipCode is required");
            return new HourlyForecastResponse(false, "", [], Errors.ZipCodeRequired);
        }

        try
        {
            // Geocode zip
            var geoUrl = $"{Urls.GoogleGeocode}?address={zipCode}&key={_apiKey}";
            _logger.LogTrace("GetHourlyForecastByZip geocoding via {Url}", geoUrl);

            var geo = await _http.GetFromJsonAsync<JsonElement>(geoUrl);
            _logger.LogTrace("GetHourlyForecastByZip received geocode response");

            if (!geo.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                _logger.LogWarning("GetHourlyForecastByZip could not geocode {ZipCode}", zipCode);
                return new HourlyForecastResponse(false, "", [], string.Format(Errors.LocationNotFoundZip, zipCode));
            }

            var loc = results[0].GetProperty("geometry").GetProperty("location");
            var lat = loc.GetProperty("lat").GetDouble();
            var lng = loc.GetProperty("lng").GetDouble();
            _logger.LogTrace("GetHourlyForecastByZip geocoded to Lat={Lat}, Lng={Lng}", lat, lng);

            // Get forecast
            var forecastUrl = $"{Urls.GoogleWeatherHourly}?location.latitude={lat}&location.longitude={lng}&hours={hours}&key={_apiKey}";
            _logger.LogTrace("GetHourlyForecastByZip fetching forecast from {Url}", forecastUrl);

            var forecast = await _http.GetFromJsonAsync<JsonElement>(forecastUrl);
            _logger.LogTrace("GetHourlyForecastByZip received forecast response");

            var forecasts = new List<HourlyForecast>();
            foreach (var hour in forecast.GetProperty("forecastHours").EnumerateArray())
            {
                var dt = hour.GetProperty("displayDateTime");
                var time = new DateTime(
                    dt.GetProperty("year").GetInt32(),
                    dt.GetProperty("month").GetInt32(),
                    dt.GetProperty("day").GetInt32(),
                    dt.GetProperty("hours").GetInt32(), 0, 0);

                forecasts.Add(new HourlyForecast(
                    time,
                    hour.GetProperty("weatherCondition").GetProperty("description").GetProperty("text").GetString() ?? "",
                    hour.GetProperty("temperature").GetProperty("degrees").GetDouble(),
                    hour.GetProperty("precipitation").GetProperty("probability").GetProperty("percent").GetInt32()));

                _logger.LogTrace("GetHourlyForecastByZip parsed forecast for {Time}", time);
            }

            _logger.LogTrace("GetHourlyForecastByZip completed with {Count} hours", forecasts.Count);
            return new HourlyForecastResponse(true, zipCode, forecasts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetHourlyForecastByZip failed with exception");
            return new HourlyForecastResponse(false, "", [], string.Format(Errors.GenericError, ex.Message));
        }
    }
}
