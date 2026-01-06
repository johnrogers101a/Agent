using System.Text.Json.Serialization;

namespace GoogleApiClient.Weather;

#region Daily Forecast Response

internal record DailyForecastResponse(
    [property: JsonPropertyName("forecastDays")] ForecastDay[] ForecastDays,
    [property: JsonPropertyName("timeZone")] TimeZoneInfo TimeZone
);

internal record ForecastDay(
    [property: JsonPropertyName("interval")] Interval Interval,
    [property: JsonPropertyName("displayDate")] DisplayDate DisplayDate,
    [property: JsonPropertyName("daytimeForecast")] DayPartForecast? DaytimeForecast,
    [property: JsonPropertyName("nighttimeForecast")] DayPartForecast? NighttimeForecast,
    [property: JsonPropertyName("maxTemperature")] Temperature MaxTemperature,
    [property: JsonPropertyName("minTemperature")] Temperature MinTemperature,
    [property: JsonPropertyName("feelsLikeMaxTemperature")] Temperature? FeelsLikeMaxTemperature,
    [property: JsonPropertyName("feelsLikeMinTemperature")] Temperature? FeelsLikeMinTemperature,
    [property: JsonPropertyName("sunEvents")] SunEvents? SunEvents
);

internal record DisplayDate(
    [property: JsonPropertyName("year")] int Year,
    [property: JsonPropertyName("month")] int Month,
    [property: JsonPropertyName("day")] int Day
);

internal record DayPartForecast(
    [property: JsonPropertyName("weatherCondition")] WeatherCondition WeatherCondition,
    [property: JsonPropertyName("relativeHumidity")] int RelativeHumidity,
    [property: JsonPropertyName("uvIndex")] int UvIndex,
    [property: JsonPropertyName("precipitation")] Precipitation? Precipitation,
    [property: JsonPropertyName("thunderstormProbability")] int ThunderstormProbability,
    [property: JsonPropertyName("wind")] Wind? Wind,
    [property: JsonPropertyName("cloudCover")] int CloudCover
);

internal record Precipitation(
    [property: JsonPropertyName("probability")] PrecipitationProbability? Probability,
    [property: JsonPropertyName("qpf")] QuantitativePrecipitation? Qpf
);

internal record PrecipitationProbability(
    [property: JsonPropertyName("percent")] int Percent,
    [property: JsonPropertyName("type")] string? Type
);

internal record QuantitativePrecipitation(
    [property: JsonPropertyName("quantity")] double Quantity,
    [property: JsonPropertyName("unit")] string Unit
);

internal record SunEvents(
    [property: JsonPropertyName("sunriseTime")] string? SunriseTime,
    [property: JsonPropertyName("sunsetTime")] string? SunsetTime
);

#endregion

#region Hourly Forecast Response

internal record HourlyForecastResponse(
    [property: JsonPropertyName("forecastHours")] ForecastHour[] ForecastHours,
    [property: JsonPropertyName("timeZone")] TimeZoneInfo TimeZone
);

internal record ForecastHour(
    [property: JsonPropertyName("interval")] Interval Interval,
    [property: JsonPropertyName("displayDateTime")] DisplayDateTime DisplayDateTime,
    [property: JsonPropertyName("isDaytime")] bool IsDaytime,
    [property: JsonPropertyName("weatherCondition")] WeatherCondition WeatherCondition,
    [property: JsonPropertyName("temperature")] Temperature Temperature,
    [property: JsonPropertyName("feelsLikeTemperature")] Temperature? FeelsLikeTemperature,
    [property: JsonPropertyName("relativeHumidity")] int RelativeHumidity,
    [property: JsonPropertyName("uvIndex")] int UvIndex,
    [property: JsonPropertyName("precipitation")] Precipitation? Precipitation,
    [property: JsonPropertyName("thunderstormProbability")] int ThunderstormProbability,
    [property: JsonPropertyName("wind")] Wind? Wind,
    [property: JsonPropertyName("cloudCover")] int CloudCover
);

internal record DisplayDateTime(
    [property: JsonPropertyName("year")] int Year,
    [property: JsonPropertyName("month")] int Month,
    [property: JsonPropertyName("day")] int Day,
    [property: JsonPropertyName("hours")] int Hours,
    [property: JsonPropertyName("utcOffset")] string? UtcOffset
);

#endregion

#region Shared

internal record Interval(
    [property: JsonPropertyName("startTime")] string StartTime,
    [property: JsonPropertyName("endTime")] string EndTime
);

internal record TimeZoneInfo(
    [property: JsonPropertyName("id")] string Id
);

#endregion
