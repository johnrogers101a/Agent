namespace GoogleTools.Clients.GoogleWeather;

public interface IGoogleWeatherService
{
    /// <summary>
    /// Gets latitude/longitude from a US zip code using Google Geocoding API.
    /// </summary>
    Task<GeoLocation?> GetLocationByZipAsync(string zipCode);

    /// <summary>
    /// Gets current weather conditions for the specified coordinates using Google Weather API.
    /// </summary>
    Task<WeatherResult?> GetWeatherAsync(double latitude, double longitude);
}
