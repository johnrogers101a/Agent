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

    #region Current Weather

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

    #endregion

    #region Daily Forecast

    public static async Task<LocationDailyForecastResult?> GetDailyForecastByZip(
        [Description("US zip code")] string zipCode,
        [Description("Number of days (1-10, default 5)")] int days = 5)
    {
        var location = await _weatherService.GetLocationByZipAsync(zipCode);
        if (location is null)
            return null;

        var forecast = await _weatherService.GetDailyForecastAsync(location.Latitude, location.Longitude, days);
        if (forecast is null)
            return null;

        return new LocationDailyForecastResult(zipCode, forecast);
    }

    public static async Task<LocationDailyForecastResult?> GetDailyForecastByCityState(
        [Description("City name")] string city,
        [Description("State name or abbreviation")] string state,
        [Description("Number of days (1-10, default 5)")] int days = 5)
    {
        var location = await _nominatimService.GetLocationAsync(city, state);
        if (location is null)
            return null;

        var forecast = await _weatherService.GetDailyForecastAsync(location.Latitude, location.Longitude, days);
        if (forecast is null)
            return null;

        return new LocationDailyForecastResult($"{city}, {state}", forecast);
    }

    public static AIFunction CreateGetDailyForecastByZip(string description)
    {
        return AIFunctionFactory.Create(
            GetDailyForecastByZip,
            name: "GetDailyForecastByZip",
            description: description);
    }

    public static AIFunction CreateGetDailyForecastByCityState(string description)
    {
        return AIFunctionFactory.Create(
            GetDailyForecastByCityState,
            name: "GetDailyForecastByCityState",
            description: description);
    }

    #endregion

    #region Hourly Forecast

    public static async Task<LocationHourlyForecastResult?> GetHourlyForecastByZip(
        [Description("US zip code")] string zipCode,
        [Description("Number of hours (1-240, default 24)")] int hours = 24)
    {
        var location = await _weatherService.GetLocationByZipAsync(zipCode);
        if (location is null)
            return null;

        var forecast = await _weatherService.GetHourlyForecastAsync(location.Latitude, location.Longitude, hours);
        if (forecast is null)
            return null;

        return new LocationHourlyForecastResult(zipCode, forecast);
    }

    public static async Task<LocationHourlyForecastResult?> GetHourlyForecastByCityState(
        [Description("City name")] string city,
        [Description("State name or abbreviation")] string state,
        [Description("Number of hours (1-240, default 24)")] int hours = 24)
    {
        var location = await _nominatimService.GetLocationAsync(city, state);
        if (location is null)
            return null;

        var forecast = await _weatherService.GetHourlyForecastAsync(location.Latitude, location.Longitude, hours);
        if (forecast is null)
            return null;

        return new LocationHourlyForecastResult($"{city}, {state}", forecast);
    }

    public static AIFunction CreateGetHourlyForecastByZip(string description)
    {
        return AIFunctionFactory.Create(
            GetHourlyForecastByZip,
            name: "GetHourlyForecastByZip",
            description: description);
    }

    public static AIFunction CreateGetHourlyForecastByCityState(string description)
    {
        return AIFunctionFactory.Create(
            GetHourlyForecastByCityState,
            name: "GetHourlyForecastByCityState",
            description: description);
    }

    #endregion
}
