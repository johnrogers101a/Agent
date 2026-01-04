using System.Net.Http.Json;

namespace GoogleTools.Clients.GoogleWeather;

public class GoogleWeatherService : IGoogleWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    private const string GeocodingBaseUrl = "https://maps.googleapis.com/maps/api/geocode/json";
    private const string WeatherBaseUrl = "https://weather.googleapis.com/v1/currentConditions:lookup";

    public GoogleWeatherService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<GeoLocation?> GetLocationByZipAsync(string zipCode)
    {
        var url = $"{GeocodingBaseUrl}?components=postal_code:{zipCode}|country:US&key={_apiKey}";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<GeocodingResponse>();
        if (result?.Status != "OK" || result.Results.Length == 0)
            return null;

        var location = result.Results[0].Geometry.Location;
        return new GeoLocation(location.Lat, location.Lng);
    }

    public async Task<WeatherResult?> GetWeatherAsync(double latitude, double longitude)
    {
        var url = $"{WeatherBaseUrl}?key={_apiKey}&location.latitude={latitude}&location.longitude={longitude}&unitsSystem=IMPERIAL";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<CurrentConditionsResponse>();
        if (result is null)
            return null;

        return new WeatherResult(
            Description: result.WeatherCondition.Description.Text,
            Temperature: result.Temperature.Degrees,
            FeelsLike: result.FeelsLikeTemperature.Degrees,
            TemperatureUnit: result.Temperature.Unit,
            Humidity: result.RelativeHumidity,
            WindSpeed: result.Wind.Speed.Value,
            WindDirection: result.Wind.Direction.Cardinal,
            UvIndex: result.UvIndex,
            IconUrl: $"{result.WeatherCondition.IconBaseUri}.svg",
            RetrievedAt: DateTime.UtcNow
        );
    }
}
