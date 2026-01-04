using System.ComponentModel;
using GoogleTools.Clients.GoogleWeather;
using GoogleTools.Clients.NominatimCityState;
using GoogleTools.Models;
using AgentFramework.Configuration;
using Microsoft.Extensions.AI;

namespace GoogleTools.Tools;

public static class WeatherTool
{
    private static readonly AppSettings _settings = AppSettings.LoadConfiguration();
    private static readonly HttpClient _httpClient = new();
    private static readonly IGoogleWeatherService _weatherService = new GoogleWeatherService(_httpClient, _settings.Clients.GoogleWeather.ApiKey);
    private static readonly INominatimCityStateService _nominatimService = new NominatimCityStateService(_httpClient);

    [Description("Gets the current weather for a US zip code")]
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

    [Description("Gets the current weather for a US city and state")]
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

    public static AIFunction CreateGetWeatherByZip()
    {
        return AIFunctionFactory.Create(
            GetWeatherByZip,
            name: "GetWeatherByZip",
            description: "Gets the current weather for a US zip code");
    }

    public static AIFunction CreateGetWeatherByCityState()
    {
        return AIFunctionFactory.Create(
            GetWeatherByCityState,
            name: "GetWeatherByCityState",
            description: "Gets the current weather for a US city and state");
    }
}
