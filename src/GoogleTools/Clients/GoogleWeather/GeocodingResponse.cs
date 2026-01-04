using System.Text.Json.Serialization;

namespace GoogleTools.Clients.GoogleWeather;

internal record GeocodingResponse(
    [property: JsonPropertyName("results")] GeocodingResult[] Results,
    [property: JsonPropertyName("status")] string Status
);

internal record GeocodingResult(
    [property: JsonPropertyName("geometry")] Geometry Geometry
);

internal record Geometry(
    [property: JsonPropertyName("location")] Location Location
);

internal record Location(
    [property: JsonPropertyName("lat")] double Lat,
    [property: JsonPropertyName("lng")] double Lng
);
