using System.Text.Json.Serialization;

namespace BlazorSseClient.Demo.Api.Weather.Data
{
    internal sealed class ReadingCurrentWeather
    {
        [JsonPropertyName("time")]
        public DateTime Time { get; init; }

        [JsonPropertyName("interval")]
        public int IntervalSeconds { get; init; }

        [JsonPropertyName("temperature_2m")]
        public double Temperature2m { get; init; }

        [JsonPropertyName("relative_humidity_2m")]
        public int RelativeHumidity2m { get; init; }

        [JsonPropertyName("is_day")]
        public int IsDayRaw { get; init; }

        [JsonIgnore]
        public bool IsDay => IsDayRaw == 1;

        [JsonPropertyName("wind_speed_10m")]
        public double WindSpeed10m { get; init; }

        [JsonPropertyName("wind_direction_10m")]
        public int WindDirection10m { get; init; }

        [JsonPropertyName("wind_gusts_10m")]
        public double WindGusts10m { get; init; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; init; }

        [JsonPropertyName("precipitation")]
        public double Precipitation { get; init; }

        [JsonPropertyName("apparent_temperature")]
        public double ApparentTemperature { get; init; }
    }
}
