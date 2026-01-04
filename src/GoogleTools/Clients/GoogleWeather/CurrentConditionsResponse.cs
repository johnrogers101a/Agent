using System.Text.Json.Serialization;

namespace GoogleTools.Clients.GoogleWeather;

internal record CurrentConditionsResponse(
    [property: JsonPropertyName("currentTime")] string CurrentTime,
    [property: JsonPropertyName("isDaytime")] bool IsDaytime,
    [property: JsonPropertyName("weatherCondition")] WeatherCondition WeatherCondition,
    [property: JsonPropertyName("temperature")] Temperature Temperature,
    [property: JsonPropertyName("feelsLikeTemperature")] Temperature FeelsLikeTemperature,
    [property: JsonPropertyName("relativeHumidity")] int RelativeHumidity,
    [property: JsonPropertyName("uvIndex")] int UvIndex,
    [property: JsonPropertyName("wind")] Wind Wind
);

internal record WeatherCondition(
    [property: JsonPropertyName("iconBaseUri")] string IconBaseUri,
    [property: JsonPropertyName("description")] Description Description,
    [property: JsonPropertyName("type")] string Type
);

internal record Description(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("languageCode")] string LanguageCode
);

internal record Temperature(
    [property: JsonPropertyName("degrees")] double Degrees,
    [property: JsonPropertyName("unit")] string Unit
);

internal record Wind(
    [property: JsonPropertyName("direction")] WindDirection Direction,
    [property: JsonPropertyName("speed")] Speed Speed
);

internal record WindDirection(
    [property: JsonPropertyName("degrees")] int Degrees,
    [property: JsonPropertyName("cardinal")] string Cardinal
);

internal record Speed(
    [property: JsonPropertyName("value")] double Value,
    [property: JsonPropertyName("unit")] string Unit
);
