namespace GoogleApiClient.Weather;

public interface IWeatherService
{
    /// <summary>
    /// Gets latitude/longitude from a US zip code using Google Geocoding API.
    /// </summary>
    Task<GeoLocation?> GetLocationByZipAsync(string zipCode);

    /// <summary>
    /// Gets current weather conditions for the specified coordinates using Google Weather API.
    /// </summary>
    Task<WeatherResult?> GetWeatherAsync(double latitude, double longitude);

    /// <summary>
    /// Gets daily weather forecast for the specified coordinates using Google Weather API.
    /// </summary>
    Task<List<ForecastDayResult>?> GetDailyForecastAsync(double latitude, double longitude, int days = 5);

    /// <summary>
    /// Gets hourly weather forecast for the specified coordinates using Google Weather API.
    /// </summary>
    Task<List<ForecastHourResult>?> GetHourlyForecastAsync(double latitude, double longitude, int hours = 24);
}
