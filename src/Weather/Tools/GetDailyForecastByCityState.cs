#nullable enable

using System.Net.Http.Json;
using System.Text.Json;
using AgentFramework.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Weather.Models;

namespace Weather.Tools;

/// <summary>
/// Gets daily weather forecast for a US city and state using Google Weather API.
/// </summary>
public class GetDailyForecastByCityState
{
    private readonly HttpClient _http;
    private readonly ILogger<GetDailyForecastByCityState> _logger;
    private readonly string _apiKey;

    public GetDailyForecastByCityState(HttpClient httpClient, IConfiguration config, ILogger<GetDailyForecastByCityState> logger)
    {
        _http = httpClient;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(Defaults.UserAgent);
        _logger = logger;
        _apiKey = config[ConfigKeys.ApiKey] ?? throw new InvalidOperationException(Errors.ApiKeyNotConfigured);
    }

    /// <summary>
    /// Gets daily weather forecast for a US city and state.
    /// </summary>
    /// <param name="city">City name (e.g., Seattle).</param>
    /// <param name="state">State (e.g., WA).</param>
    /// <param name="days">Number of days (1-10).</param>
    [McpTool]
    public async Task<DailyForecastResponse> ExecuteAsync(string city, string state, int days)
    {
        days = Math.Clamp(days, 1, 10);
        _logger.LogTrace("GetDailyForecastByCityState starting for City={City}, State={State}, Days={Days}", city, state, days);

        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning("GetDailyForecastByCityState failed: City and State are required");
            return new DailyForecastResponse(false, "", [], Errors.CityStateRequired);
        }

        try
        {
            // Geocode via Nominatim
            var geoUrl = $"{Urls.Nominatim}?q={Uri.EscapeDataString($"{city}, {state}, USA")}&format=json&limit=1";
            _logger.LogTrace("GetDailyForecastByCityState geocoding via {Url}", geoUrl);

            var geo = await _http.GetFromJsonAsync<JsonElement>(geoUrl);
            _logger.LogTrace("GetDailyForecastByCityState received geocode response");

            if (geo.ValueKind != JsonValueKind.Array || geo.GetArrayLength() == 0)
            {
                _logger.LogWarning("GetDailyForecastByCityState could not geocode {City}, {State}", city, state);
                return new DailyForecastResponse(false, "", [], string.Format(Errors.LocationNotFoundCityState, city, state));
            }

            var lat = geo[0].GetProperty("lat").GetString();
            var lng = geo[0].GetProperty("lon").GetString();
            _logger.LogTrace("GetDailyForecastByCityState geocoded to Lat={Lat}, Lng={Lng}", lat, lng);

            // Get forecast
            var forecastUrl = $"{Urls.GoogleWeatherDaily}?location.latitude={lat}&location.longitude={lng}&days={days}&key={_apiKey}";
            _logger.LogTrace("GetDailyForecastByCityState fetching forecast from {Url}", forecastUrl);

            var forecast = await _http.GetFromJsonAsync<JsonElement>(forecastUrl);
            _logger.LogTrace("GetDailyForecastByCityState received forecast response");

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

                _logger.LogTrace("GetDailyForecastByCityState parsed forecast for {Date}", date);
            }

            _logger.LogTrace("GetDailyForecastByCityState completed with {Count} days", forecasts.Count);
            return new DailyForecastResponse(true, $"{city}, {state}", forecasts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetDailyForecastByCityState failed with exception");
            return new DailyForecastResponse(false, "", [], string.Format(Errors.GenericError, ex.Message));
        }
    }
}
