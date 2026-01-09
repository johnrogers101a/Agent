#nullable enable

namespace Weather;

internal static class Urls
{
    public const string Nominatim = "https://nominatim.openstreetmap.org/search";
    public const string GoogleGeocode = "https://maps.googleapis.com/maps/api/geocode/json";
    public const string GoogleWeatherCurrent = "https://weather.googleapis.com/v1/currentConditions:lookup";
    public const string GoogleWeatherDaily = "https://weather.googleapis.com/v1/forecast/days:lookup";
    public const string GoogleWeatherHourly = "https://weather.googleapis.com/v1/forecast/hours:lookup";
}

internal static class ConfigKeys
{
    public const string ApiKey = "Clients:Weather:ApiKey";
}

internal static class Defaults
{
    public const string UserAgent = "AgentFramework/1.0";
    public const string UnitsSystem = "IMPERIAL";
}

internal static class Errors
{
    public const string ApiKeyNotConfigured = "Weather API key not configured";
    public const string ZipCodeRequired = "ZipCode is required";
    public const string CityStateRequired = "City and State are required";
    public const string LocationNotFoundZip = "Could not find location for zip: {0}";
    public const string LocationNotFoundCityState = "Could not find location for: {0}, {1}";
    public const string GenericError = "Error: {0}";
}
