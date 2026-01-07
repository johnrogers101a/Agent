#nullable enable

using System.Net.Http.Json;
using System.Text.Json;
using AgentFramework.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Weather.Models;

namespace Weather.Tools;

/// <summary>
/// Gets hourly weather forecast for a US city and state using Google Weather API.
/// </summary>
public class GetHourlyForecastByCityState
{
    private readonly HttpClient _http;
    private readonly ILogger<GetHourlyForecastByCityState> _logger;
    private readonly string _apiKey;

    public GetHourlyForecastByCityState(HttpClient httpClient, IConfiguration config, ILogger<GetHourlyForecastByCityState> logger)
    {
        _http = httpClient;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(Defaults.UserAgent);
        _logger = logger;
        _apiKey = config[ConfigKeys.ApiKey] ?? throw new InvalidOperationException(Errors.ApiKeyNotConfigured);
    }

    /// <summary>
    /// Gets hourly weather forecast for a US city and state.
    /// </summary>
    /// <param name="city">City name (e.g., Seattle).</param>
    /// <param name="state">State (e.g., WA).</param>
    /// <param name="hours">Number of hours (1-24).</param>
    [McpTool]
    public async Task<HourlyForecastResponse> ExecuteAsync(string city, string state, int hours)
    {
        hours = Math.Clamp(hours, 1, 24);
        _logger.LogTrace("GetHourlyForecastByCityState starting for City={City}, State={State}, Hours={Hours}", city, state, hours);

        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning("GetHourlyForecastByCityState failed: City and State are required");
            return new HourlyForecastResponse(false, "", [], Errors.CityStateRequired);
        }

        try
        {
            // Geocode via Nominatim
            var geoUrl = $"{Urls.Nominatim}?q={Uri.EscapeDataString($"{city}, {state}, USA")}&format=json&limit=1";
            _logger.LogTrace("GetHourlyForecastByCityState geocoding via {Url}", geoUrl);

            var geo = await _http.GetFromJsonAsync<JsonElement>(geoUrl);
            _logger.LogTrace("GetHourlyForecastByCityState received geocode response");

            if (geo.ValueKind != JsonValueKind.Array || geo.GetArrayLength() == 0)
            {
                _logger.LogWarning("GetHourlyForecastByCityState could not geocode {City}, {State}", city, state);
                return new HourlyForecastResponse(false, "", [], string.Format(Errors.LocationNotFoundCityState, city, state));
            }

            var lat = geo[0].GetProperty("lat").GetString();
            var lng = geo[0].GetProperty("lon").GetString();
            _logger.LogTrace("GetHourlyForecastByCityState geocoded to Lat={Lat}, Lng={Lng}", lat, lng);

            // Get forecast
            var forecastUrl = $"{Urls.GoogleWeatherHourly}?location.latitude={lat}&location.longitude={lng}&hours={hours}&unitsSystem={Defaults.UnitsSystem}&key={_apiKey}";
            _logger.LogTrace("GetHourlyForecastByCityState fetching forecast from {Url}", forecastUrl);

            var forecast = await _http.GetFromJsonAsync<JsonElement>(forecastUrl);
            _logger.LogTrace("GetHourlyForecastByCityState received forecast response");

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

                _logger.LogTrace("GetHourlyForecastByCityState parsed forecast for {Time}", time);
            }

            _logger.LogTrace("GetHourlyForecastByCityState completed with {Count} hours", forecasts.Count);
            return new HourlyForecastResponse(true, $"{city}, {state}", forecasts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetHourlyForecastByCityState failed with exception");
            return new HourlyForecastResponse(false, "", [], string.Format(Errors.GenericError, ex.Message));
        }
    }
}
