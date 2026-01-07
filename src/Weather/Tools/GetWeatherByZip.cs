#nullable enable

using System.Net.Http.Json;
using System.Text.Json;
using AgentFramework.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Weather.Models;

namespace Weather.Tools;

/// <summary>
/// Gets current weather for a US zip code using Google Weather API.
/// </summary>
public class GetWeatherByZip
{
    private readonly HttpClient _http;
    private readonly ILogger<GetWeatherByZip> _logger;
    private readonly string _apiKey;

    public GetWeatherByZip(HttpClient httpClient, IConfiguration config, ILogger<GetWeatherByZip> logger)
    {
        _http = httpClient;
        _logger = logger;
        _apiKey = config[ConfigKeys.ApiKey] ?? throw new InvalidOperationException(Errors.ApiKeyNotConfigured);
    }

    /// <summary>
    /// Gets current weather for a US zip code.
    /// </summary>
    /// <param name="zipCode">US zip code (e.g., 98052).</param>
    [McpTool]
    public async Task<CurrentWeatherResponse> ExecuteAsync(string zipCode)
    {
        _logger.LogTrace("GetWeatherByZip starting for ZipCode={ZipCode}", zipCode);

        if (string.IsNullOrWhiteSpace(zipCode))
        {
            _logger.LogWarning("GetWeatherByZip failed: ZipCode is required");
            return new CurrentWeatherResponse(false, null, Errors.ZipCodeRequired);
        }

        try
        {
            // Geocode zip
            var geoUrl = $"{Urls.GoogleGeocode}?address={zipCode}&key={_apiKey}";
            _logger.LogTrace("GetWeatherByZip geocoding via {Url}", geoUrl);
            
            var geo = await _http.GetFromJsonAsync<JsonElement>(geoUrl);
            _logger.LogTrace("GetWeatherByZip received geocode response");

            if (!geo.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            {
                _logger.LogWarning("GetWeatherByZip could not geocode {ZipCode}", zipCode);
                return new CurrentWeatherResponse(false, null, string.Format(Errors.LocationNotFoundZip, zipCode));
            }

            var loc = results[0].GetProperty("geometry").GetProperty("location");
            var lat = loc.GetProperty("lat").GetDouble();
            var lng = loc.GetProperty("lng").GetDouble();
            _logger.LogTrace("GetWeatherByZip geocoded to Lat={Lat}, Lng={Lng}", lat, lng);

            // Get weather
            var weatherUrl = $"{Urls.GoogleWeatherCurrent}?location.latitude={lat}&location.longitude={lng}&unitsSystem={Defaults.UnitsSystem}&key={_apiKey}";
            _logger.LogTrace("GetWeatherByZip fetching weather from {Url}", weatherUrl);

            var w = await _http.GetFromJsonAsync<JsonElement>(weatherUrl);
            _logger.LogTrace("GetWeatherByZip received weather response");

            var weather = new CurrentWeather(
                zipCode,
                w.GetProperty("weatherCondition").GetProperty("description").GetProperty("text").GetString() ?? "",
                w.GetProperty("temperature").GetProperty("degrees").GetDouble(),
                w.GetProperty("feelsLikeTemperature").GetProperty("degrees").GetDouble(),
                w.GetProperty("relativeHumidity").GetInt32(),
                w.GetProperty("wind").GetProperty("speed").GetProperty("value").GetDouble(),
                w.GetProperty("wind").GetProperty("direction").GetProperty("degrees").GetInt32(),
                w.GetProperty("uvIndex").GetInt32());

            _logger.LogTrace("GetWeatherByZip completed: Conditions={Conditions}, Temp={Temp}°F", weather.Conditions, weather.Temperature);
            return new CurrentWeatherResponse(true, weather);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetWeatherByZip failed with exception");
            return new CurrentWeatherResponse(false, null, string.Format(Errors.GenericError, ex.Message));
        }
    }
}
