using System.Text.Json.Serialization;

namespace BlazorSseClient.Demo.Api.Data.Weather
{
    internal sealed class ReadingCurrentUnits
    {
        [JsonPropertyName("time")]
        public string Time { get; init; } = string.Empty;

        [JsonPropertyName("interval")]
        public string Interval { get; init; } = string.Empty;

        [JsonPropertyName("temperature_2m")]
        public string Temperature2m { get; init; } = string.Empty;

        [JsonPropertyName("relative_humidity_2m")]
        public string RelativeHumidity2m { get; init; } = string.Empty;

        [JsonPropertyName("is_day")]
        public string IsDay { get; init; } = string.Empty;

        [JsonPropertyName("wind_speed_10m")]
        public string WindSpeed10m { get; init; } = string.Empty;

        [JsonPropertyName("wind_direction_10m")]
        public string WindDirection10m { get; init; } = string.Empty;

        [JsonPropertyName("wind_gusts_10m")]
        public string WindGusts10m { get; init; } = string.Empty;

        [JsonPropertyName("weather_code")]
        public string WeatherCode { get; init; } = string.Empty;

        [JsonPropertyName("precipitation")]
        public string Precipitation { get; init; } = string.Empty;

        [JsonPropertyName("apparent_temperature")]
        public string ApparentTemperature { get; init; } = string.Empty;
    }
}
