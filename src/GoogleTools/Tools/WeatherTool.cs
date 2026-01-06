using System.ComponentModel;
using GoogleApiClient.Weather;
using GoogleApiClient.NominatimCityState;
using AgentFramework.Configuration;
using Microsoft.Extensions.AI;

namespace GoogleTools.Tools;

public static class WeatherTool
{
    private static readonly AppSettings _settings = AppSettings.LoadConfiguration();
    private static readonly HttpClient _httpClient = new();
    private static readonly IWeatherService _weatherService = new WeatherService(_httpClient, _settings.Clients.GoogleWeather.ApiKey);
    private static readonly INominatimCityStateService _nominatimService = new NominatimCityStateService(_httpClient);

    public static async Task<LocationWeatherResult?> GetWeatherByZip(
        [Description("US zip code")] string zipCode)
    {
        var location = await _weatherService.GetLocationByZipAsync(zipCode);
        if (location is null)
            return null;

        var weather = await _weatherService.GetWeatherAsync(location.Latitude, location.Longitude);
        if (weather is null)
            return null;

        return new LocationWeatherResult(zipCode, weather);
    }

    public static async Task<LocationWeatherResult?> GetWeatherByCityState(
        [Description("City name")] string city,
        [Description("State name or abbreviation")] string state)
    {
        var location = await _nominatimService.GetLocationAsync(city, state);
        if (location is null)
            return null;

        var weather = await _weatherService.GetWeatherAsync(location.Latitude, location.Longitude);
        if (weather is null)
            return null;

        return new LocationWeatherResult($"{city}, {state}", weather);
    }

    public static AIFunction CreateGetWeatherByZip(string description)
    {
        return AIFunctionFactory.Create(
            GetWeatherByZip,
            name: "GetWeatherByZip",
            description: description);
    }

    public static AIFunction CreateGetWeatherByCityState(string description)
    {
        return AIFunctionFactory.Create(
            GetWeatherByCityState,
            name: "GetWeatherByCityState",
            description: description);
    }
}
