#nullable enable

using System.Net.Http.Json;
using System.Text.Json;
using AgentFramework.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Weather.Models;

namespace Weather.Tools;

/// <summary>
/// Gets current weather for a US city and state using Google Weather API.
/// </summary>
public class GetWeatherByCityState
{
    private readonly HttpClient _http;
    private readonly ILogger<GetWeatherByCityState> _logger;
    private readonly string _apiKey;

    public GetWeatherByCityState(HttpClient httpClient, IConfiguration config, ILogger<GetWeatherByCityState> logger)
    {
        _http = httpClient;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(Defaults.UserAgent);
        _logger = logger;
        _apiKey = config[ConfigKeys.ApiKey] ?? throw new InvalidOperationException(Errors.ApiKeyNotConfigured);
    }

    /// <summary>
    /// Gets current weather for a US city and state.
    /// </summary>
    /// <param name="city">City name (e.g., Seattle).</param>
    /// <param name="state">State (e.g., WA).</param>
    [McpTool]
    public async Task<CurrentWeatherResponse> ExecuteAsync(string city, string state)
    {
        _logger.LogTrace("GetWeatherByCityState starting for City={City}, State={State}", city, state);

        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning("GetWeatherByCityState failed: City and State are required");
            return new CurrentWeatherResponse(false, null, Errors.CityStateRequired);
        }

        try
        {
            // Geocode via Nominatim
            var geoUrl = $"{Urls.Nominatim}?q={Uri.EscapeDataString($"{city}, {state}, USA")}&format=json&limit=1";
            _logger.LogTrace("GetWeatherByCityState geocoding via {Url}", geoUrl);

            var geo = await _http.GetFromJsonAsync<JsonElement>(geoUrl);
            _logger.LogTrace("GetWeatherByCityState received geocode response");

            if (geo.ValueKind != JsonValueKind.Array || geo.GetArrayLength() == 0)
            {
                _logger.LogWarning("GetWeatherByCityState could not geocode {City}, {State}", city, state);
                return new CurrentWeatherResponse(false, null, string.Format(Errors.LocationNotFoundCityState, city, state));
            }

            var lat = geo[0].GetProperty("lat").GetString();
            var lng = geo[0].GetProperty("lon").GetString();
            _logger.LogTrace("GetWeatherByCityState geocoded to Lat={Lat}, Lng={Lng}", lat, lng);

            // Get weather
            var weatherUrl = $"{Urls.GoogleWeatherCurrent}?location.latitude={lat}&location.longitude={lng}&unitsSystem={Defaults.UnitsSystem}&key={_apiKey}";
            _logger.LogTrace("GetWeatherByCityState fetching weather from {Url}", weatherUrl);

            var w = await _http.GetFromJsonAsync<JsonElement>(weatherUrl);
            _logger.LogTrace("GetWeatherByCityState received weather response");

            var weather = new CurrentWeather(
                $"{city}, {state}",
                w.GetProperty("weatherCondition").GetProperty("description").GetProperty("text").GetString() ?? "",
                w.GetProperty("temperature").GetProperty("degrees").GetDouble(),
                w.GetProperty("feelsLikeTemperature").GetProperty("degrees").GetDouble(),
                w.GetProperty("relativeHumidity").GetInt32(),
                w.GetProperty("wind").GetProperty("speed").GetProperty("value").GetDouble(),
                w.GetProperty("wind").GetProperty("direction").GetProperty("degrees").GetInt32(),
                w.GetProperty("uvIndex").GetInt32());

            _logger.LogTrace("GetWeatherByCityState completed: Conditions={Conditions}, Temp={Temp}°F", weather.Conditions, weather.Temperature);
            return new CurrentWeatherResponse(true, weather);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetWeatherByCityState failed with exception");
            return new CurrentWeatherResponse(false, null, string.Format(Errors.GenericError, ex.Message));
        }
    }
}
