using System.Net.Http.Json;

namespace GoogleApiClient.Weather;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    private const string GeocodingBaseUrl = "https://maps.googleapis.com/maps/api/geocode/json";
    private const string WeatherBaseUrl = "https://weather.googleapis.com/v1/currentConditions:lookup";
    private const string DailyForecastBaseUrl = "https://weather.googleapis.com/v1/forecast/days:lookup";
    private const string HourlyForecastBaseUrl = "https://weather.googleapis.com/v1/forecast/hours:lookup";

    public WeatherService(HttpClient httpClient, string apiKey)
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

    public async Task<List<ForecastDayResult>?> GetDailyForecastAsync(double latitude, double longitude, int days = 5)
    {
        var url = $"{DailyForecastBaseUrl}?key={_apiKey}&location.latitude={latitude}&location.longitude={longitude}&days={days}&unitsSystem=IMPERIAL";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<DailyForecastResponse>();
        if (result?.ForecastDays is null || result.ForecastDays.Length == 0)
            return null;

        return result.ForecastDays.Select(day => new ForecastDayResult(
            Date: new DateOnly(day.DisplayDate.Year, day.DisplayDate.Month, day.DisplayDate.Day),
            Condition: day.DaytimeForecast?.WeatherCondition.Description.Text ?? "Unknown",
            HighTemp: day.MaxTemperature.Degrees,
            LowTemp: day.MinTemperature.Degrees,
            TempUnit: day.MaxTemperature.Unit,
            PrecipChance: day.DaytimeForecast?.Precipitation?.Probability?.Percent ?? 0,
            Humidity: day.DaytimeForecast?.RelativeHumidity ?? 0,
            WindSpeed: day.DaytimeForecast?.Wind?.Speed.Value ?? 0,
            WindDirection: day.DaytimeForecast?.Wind?.Direction.Cardinal ?? "N/A",
            UvIndex: day.DaytimeForecast?.UvIndex ?? 0
        )).ToList();
    }

    public async Task<List<ForecastHourResult>?> GetHourlyForecastAsync(double latitude, double longitude, int hours = 24)
    {
        var url = $"{HourlyForecastBaseUrl}?key={_apiKey}&location.latitude={latitude}&location.longitude={longitude}&hours={hours}&unitsSystem=IMPERIAL";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<HourlyForecastResponse>();
        if (result?.ForecastHours is null || result.ForecastHours.Length == 0)
            return null;

        return result.ForecastHours.Select(hour => new ForecastHourResult(
            DateTime: new DateTime(hour.DisplayDateTime.Year, hour.DisplayDateTime.Month, hour.DisplayDateTime.Day, hour.DisplayDateTime.Hours, 0, 0),
            Condition: hour.WeatherCondition.Description.Text,
            Temp: hour.Temperature.Degrees,
            FeelsLike: hour.FeelsLikeTemperature?.Degrees ?? hour.Temperature.Degrees,
            TempUnit: hour.Temperature.Unit,
            PrecipChance: hour.Precipitation?.Probability?.Percent ?? 0,
            Humidity: hour.RelativeHumidity,
            WindSpeed: hour.Wind?.Speed.Value ?? 0,
            WindDirection: hour.Wind?.Direction.Cardinal ?? "N/A",
            UvIndex: hour.UvIndex
        )).ToList();
    }
}
