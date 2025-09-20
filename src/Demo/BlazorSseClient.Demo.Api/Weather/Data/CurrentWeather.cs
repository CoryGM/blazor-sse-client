namespace BlazorSseClient.Demo.Api.Weather.Data
{
    public readonly record struct CurrentWeather
    {
        public required string City { get; init; }
        public required double Latitude { get; init; }
        public required double Longitude { get; init; }
        public required string Temperature { get; init; }
        public required string RelativeHumidity { get; init; }
        public required string ApparentTemperature { get; init; }
        public required bool IsDayTime { get; init; }
        public required string WindSpeed { get; init; }
        public required string WindDirection { get; init; }
        public required string WindGusts { get; init; }
        public required string Precipitation { get; init; }
        public required DateTime TakenAtUtc { get; init; }
    }
}
