using System.Text.Json.Serialization;

namespace BlazorSseClient.Demo.Api.Weather.Data
{
    internal sealed class ReadingCurrentUnits
    {
        [JsonPropertyName("time")]
        public string Time { get; init; } = String.Empty;

        [JsonPropertyName("interval")]
        public string Interval { get; init; } = String.Empty;

        [JsonPropertyName("temperature_2m")]
        public string Temperature2m { get; init; } = String.Empty;

        [JsonPropertyName("relative_humidity_2m")]
        public string RelativeHumidity2m { get; init; } = String.Empty;

        [JsonPropertyName("is_day")]
        public string IsDay { get; init; } = String.Empty;

        [JsonPropertyName("wind_speed_10m")]
        public string WindSpeed10m { get; init; } = String.Empty;

        [JsonPropertyName("wind_direction_10m")]
        public string WindDirection10m { get; init; } = String.Empty;

        [JsonPropertyName("wind_gusts_10m")]
        public string WindGusts10m { get; init; } = String.Empty;

        [JsonPropertyName("weather_code")]
        public string WeatherCode { get; init; } = String.Empty;

        [JsonPropertyName("precipitation")]
        public string Precipitation { get; init; } = String.Empty;

        [JsonPropertyName("apparent_temperature")]
        public string ApparentTemperature { get; init; } = String.Empty;
    }
}
