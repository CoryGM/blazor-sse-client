namespace BlazorSseClient.Demo.Components.Weather
{
    public struct WeatherSseModel
    {
        public string City { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public string Temperature { get; init; }
        public string RelativeHumidity { get; init; }
        public string ApparentTemperature { get; init; }
        public bool IsDayTime { get; init; }
        public string WindSpeed { get; init; }
        public string WindDirection { get; init; }
        public string WindGusts { get; init; }
        public string Precipitation { get; init; }
        public required DateTime TakenAtUtc { get; init; }
        public int ReadingNumber { get; init; }
    }
}
