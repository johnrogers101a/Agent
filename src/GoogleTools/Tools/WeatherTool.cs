using System.ComponentModel;
using GoogleApiClient.Weather;
using GoogleApiClient.NominatimCityState;
using GoogleApiClient.Models;
using AgentFramework.Configuration;
using Microsoft.Extensions.AI;

namespace GoogleTools.Tools;

public static class WeatherTool
{
    private static readonly AppSettings _settings = AppSettings.LoadConfiguration();
    private static readonly HttpClient _httpClient = new();
    private static readonly IWeatherService _weatherService = new WeatherService(_httpClient, _settings.Clients.GoogleWeather.ApiKey);
    private static readonly INominatimCityStateService _nominatimService = new NominatimCityStateService(_httpClient);

    public static async Task<WeatherToolResult?> GetWeatherByZip(
        [Description("The US zip code to get weather for")] string zipCode)
    {
        // Step 1: Get coordinates from zip code via Google Geocoding
        var location = await _weatherService.GetLocationByZipAsync(zipCode);
        if (location is null)
            return null;

        // Step 2: Get weather for coordinates via Google Weather
        var weather = await _weatherService.GetWeatherAsync(location.Latitude, location.Longitude);
        if (weather is null)
            return null;

        return new WeatherToolResult(
            Location: zipCode,
            Description: weather.Description,
            Temperature: weather.Temperature,
            FeelsLike: weather.FeelsLike,
            TemperatureUnit: weather.TemperatureUnit,
            Humidity: weather.Humidity,
            WindSpeed: weather.WindSpeed,
            WindDirection: weather.WindDirection,
            UvIndex: weather.UvIndex);
    }

    public static async Task<WeatherToolResult?> GetWeatherByCityState(
        [Description("The city name")] string city,
        [Description("The state name or abbreviation")] string state)
    {
        // Step 1: Get coordinates from city/state via Nominatim
        var location = await _nominatimService.GetLocationAsync(city, state);
        if (location is null)
            return null;

        // Step 2: Get weather for coordinates via Google Weather
        var weather = await _weatherService.GetWeatherAsync(location.Latitude, location.Longitude);
        if (weather is null)
            return null;

        return new WeatherToolResult(
            Location: $"{city}, {state}",
            Description: weather.Description,
            Temperature: weather.Temperature,
            FeelsLike: weather.FeelsLike,
            TemperatureUnit: weather.TemperatureUnit,
            Humidity: weather.Humidity,
            WindSpeed: weather.WindSpeed,
            WindDirection: weather.WindDirection,
            UvIndex: weather.UvIndex);
    }

    public static AIFunction CreateGetWeatherByZip(string? description = null)
    {
        return AIFunctionFactory.Create(
            GetWeatherByZip,
            name: "GetWeatherByZip",
            description: description ?? "Gets the current weather for a US zip code");
    }

    public static AIFunction CreateGetWeatherByCityState(string? description = null)
    {
        return AIFunctionFactory.Create(
            GetWeatherByCityState,
            name: "GetWeatherByCityState",
            description: description ?? "Gets the current weather for a US city and state");
    }
}
