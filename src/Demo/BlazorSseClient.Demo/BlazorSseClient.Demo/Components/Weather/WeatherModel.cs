using BlazorSseClient.Demo.Shared.Extensions;

namespace BlazorSseClient.Demo.Components.Weather
{
    public class WeatherModel
    {
        public string? City { get; init; }
        public string? Temperature { get; init; }
        public string? RelativeHumidity { get; init; }
        public string? ApparentTemperature { get; init; }
        public bool IsDayTime { get; init; }
        public string? WindSpeed { get; init; }
        public string? WindDirection { get; init; }
        public string? WindGusts { get; init; }
        public string? Precipitation { get; init; }
        public DateTime ReportedAtUtc { get; init; } = DateTime.UtcNow;
        public string ReportedAgo { get => ReportedAtUtc.ToReadableDuration(DateTime.UtcNow, 2, LabelStyle.Short); }
        public bool IsLastReported { get; set; } = true;
        public int ReadingsCount { get; set; } = 0;
    }
}
