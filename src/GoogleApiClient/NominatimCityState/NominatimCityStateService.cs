using System.Net.Http.Json;
using GoogleApiClient.Weather;

namespace GoogleApiClient.NominatimCityState;

public class NominatimCityStateService : INominatimCityStateService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://nominatim.openstreetmap.org/search";

    public NominatimCityStateService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        // Nominatim requires a User-Agent header per their usage policy
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AgentApp/1.0");
        }
    }

    public async Task<GeoLocation?> GetLocationAsync(string city, string state)
    {
        var url = $"{BaseUrl}?city={Uri.EscapeDataString(city)}&state={Uri.EscapeDataString(state)}&country=USA&format=json&limit=1";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        var results = await response.Content.ReadFromJsonAsync<NominatimCityStateResponse[]>();
        if (results is null || results.Length == 0)
            return null;

        if (double.TryParse(results[0].Lat, out var lat) &&
            double.TryParse(results[0].Lon, out var lon))
        {
            return new GeoLocation(lat, lon);
        }

        return null;
    }
}
