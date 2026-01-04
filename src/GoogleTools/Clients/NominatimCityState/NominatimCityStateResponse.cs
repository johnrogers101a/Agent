using System.Text.Json.Serialization;

namespace GoogleTools.Clients.NominatimCityState;

internal record NominatimCityStateResponse(
    [property: JsonPropertyName("lat")] string Lat,
    [property: JsonPropertyName("lon")] string Lon,
    [property: JsonPropertyName("display_name")] string DisplayName
);
