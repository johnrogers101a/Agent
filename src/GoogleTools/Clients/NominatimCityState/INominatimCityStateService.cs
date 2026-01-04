using GoogleTools.Clients.GoogleWeather;

namespace GoogleTools.Clients.NominatimCityState;

public interface INominatimCityStateService
{
    /// <summary>
    /// Gets latitude/longitude from a city and state using Nominatim (OpenStreetMap) API.
    /// </summary>
    Task<GeoLocation?> GetLocationAsync(string city, string state);
}
