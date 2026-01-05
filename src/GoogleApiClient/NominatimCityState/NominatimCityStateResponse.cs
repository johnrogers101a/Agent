using System.Text.Json.Serialization;

namespace GoogleApiClient.NominatimCityState;

internal record NominatimCityStateResponse(
    [property: JsonPropertyName("lat")] string Lat,
    [property: JsonPropertyName("lon")] string Lon,
    [property: JsonPropertyName("display_name")] string DisplayName
);
