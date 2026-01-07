#nullable enable

using System.Net.Http.Json;
using System.Text.Json;
using AgentFramework.Attributes;
using Microsoft.Extensions.Logging;
using Weather.Models;

namespace Weather.Tools;

/// <summary>
/// Gets latitude/longitude coordinates for a US city and state using Nominatim.
/// </summary>
public class GetLocation
{
    private readonly HttpClient _http;
    private readonly ILogger<GetLocation> _logger;

    public GetLocation(HttpClient httpClient, ILogger<GetLocation> logger)
    {
        _http = httpClient;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(Defaults.UserAgent);
        _logger = logger;
    }

    /// <summary>
    /// Gets latitude/longitude coordinates for a US city and state.
    /// </summary>
    /// <param name="city">City name (e.g., Seattle).</param>
    /// <param name="state">State (e.g., WA).</param>
    [McpTool]
    public async Task<LocationResponse> ExecuteAsync(string city, string state)
    {
        _logger.LogTrace("GetLocation starting for City={City}, State={State}", city, state);

        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning("GetLocation failed: City and State are required");
            return new LocationResponse(false, 0, 0, Errors.CityStateRequired);
        }

        var url = $"{Urls.Nominatim}?q={Uri.EscapeDataString($"{city}, {state}, USA")}&format=json&limit=1";
        _logger.LogTrace("GetLocation requesting {Url}", url);

        try
        {
            var json = await _http.GetFromJsonAsync<JsonElement>(url);
            _logger.LogTrace("GetLocation received response");

            if (json.ValueKind != JsonValueKind.Array || json.GetArrayLength() == 0)
            {
                _logger.LogWarning("GetLocation found no results for {City}, {State}", city, state);
                return new LocationResponse(false, 0, 0, string.Format(Errors.LocationNotFoundCityState, city, state));
            }

            var result = json[0];
            var lat = double.Parse(result.GetProperty("lat").GetString()!);
            var lon = double.Parse(result.GetProperty("lon").GetString()!);

            _logger.LogTrace("GetLocation found coordinates: Lat={Lat}, Lon={Lon}", lat, lon);
            return new LocationResponse(true, lat, lon);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetLocation failed with exception");
            return new LocationResponse(false, 0, 0, string.Format(Errors.GenericError, ex.Message));
        }
    }
}
