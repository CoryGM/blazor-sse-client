using System.Text.Json.Serialization;

namespace BlazorSseClient.Demo.Api.Data.Weather
{
    internal sealed class Reading
    {
        [JsonIgnore]
        public string? LocationName { get; set; }  

        [JsonPropertyName("latitude")]
        public double Latitude { get; init; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; init; }

        [JsonPropertyName("generationtime_ms")]
        public double GenerationTimeMs { get; init; }

        [JsonPropertyName("utc_offset_seconds")]
        public int UtcOffsetSeconds { get; init; }

        [JsonPropertyName("timezone")]
        public string TimeZone { get; init; } = string.Empty;

        [JsonPropertyName("timezone_abbreviation")]
        public string TimeZoneAbbreviation { get; init; } = string.Empty;

        [JsonPropertyName("elevation")]
        public double Elevation { get; init; }

        [JsonPropertyName("current_units")]
        public ReadingCurrentUnits Units { get; init; } = new();

        [JsonPropertyName("current")]
        public ReadingCurrentWeather Current { get; init; } = new();
    }
}
