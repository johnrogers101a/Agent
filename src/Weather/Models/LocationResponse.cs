#nullable enable

namespace Weather.Models;

public record LocationResponse(bool Success, double Latitude, double Longitude, string? Error = null);
