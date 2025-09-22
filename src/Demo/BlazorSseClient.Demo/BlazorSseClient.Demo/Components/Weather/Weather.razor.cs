using System.Text.Json;
using BlazorSseClient.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorSseClient.Demo.Components.Weather
{
    public partial class Weather : ComponentBase, IAsyncDisposable
    {
        [Inject]
        private ISseClient SseClient { get; set; } = null!;

        [Inject]
        private HttpClient HttpClient { get; set; } = null!;

        private readonly List<WeatherModel> _readings = [];
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private Guid? _weatherSubscriptionId;
        private const string _messageType = "Weather";
        private System.Timers.Timer? _timer;
        private string renderLocation = "Server";

        protected override void OnInitialized()
        {
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (sender, e) => InvokeAsync(StateHasChanged);
            _timer.AutoReset = true;
            _timer.Enabled = true;

            if (OperatingSystem.IsBrowser())
                renderLocation = "Browser";
            else
                renderLocation = "Server";
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await GetMostRecentReadingsAsync();
                _weatherSubscriptionId = SseClient.Subscribe(_messageType, AddReading);
            }
        }

        private void AddReading(SseEvent sseEvent)
        {
            if (OperatingSystem.IsBrowser())
            {
                renderLocation = "Browser";
                Console.WriteLine($"AddReading() executing in Browser.");
            }
            else
            {
                renderLocation = "Server";
                Console.WriteLine($"AddReading() executing on Server.");
            }

            try
            {
                Console.WriteLine($"AddReading called. Data: {sseEvent.Data}");
                var currentWeather = JsonSerializer.Deserialize<WeatherSseModel?>(sseEvent.Data, _jsonOptions);

                if (currentWeather == null)
                {
                    Console.WriteLine("Deserialized currentWeather is null.");
                    return;
                }
                
                var reading = new WeatherModel
                {
                    City = currentWeather?.City ?? "Unknown",
                    Temperature = currentWeather?.Temperature ?? "N/A",
                    RelativeHumidity = currentWeather?.RelativeHumidity ?? "N/A",
                    ApparentTemperature = currentWeather?.ApparentTemperature ?? "N/A",
                    IsDayTime = currentWeather?.IsDayTime ?? false,
                    WindSpeed = currentWeather?.WindSpeed ?? "N/A",
                    WindDirection = currentWeather?.WindDirection ?? "N/A",
                    WindGusts = currentWeather?.WindGusts ?? "N/A",
                    Precipitation = currentWeather?.Precipitation ?? "N/A",
                    ReadingNumber = currentWeather?.ReadingNumber ?? 1,
                    ReportedAtUtc = currentWeather?.TakenAtUtc ?? DateTime.UtcNow
                };

                AddReading(reading);
            }
            catch (JsonException)
            {
                // Log or handle the error as needed
                return;
            }

            Console.WriteLine($"Before StateHasChanged. readings.Count: {_readings.Count}");
            InvokeAsync(StateHasChanged);
            Console.WriteLine("After StateHasChanged");
        }

        private void AddReading(WeatherModel reading)
        {
            var existingReading = _readings.Find(r => r.City == reading.City);

            if (existingReading is null)
            {
                _readings.Add(reading);
            }
            else
            {
                _readings.RemoveAll(r => r.City == reading.City);
                _readings.Add(reading);
            }

            _readings.Sort((a, b) => String.Compare(a.City, b.City, StringComparison.OrdinalIgnoreCase));

            var lastReportedUtc = _readings.Max(r => r.ReportedAtUtc);

            foreach (var r in _readings)
            {
                r.IsLastReported = r.ReportedAtUtc == lastReportedUtc;
            }
        }

        private async Task GetMostRecentReadingsAsync()
        {
            var response = await HttpClient.GetAsync("api/weather/readings/most-recent");

            if (!response.IsSuccessStatusCode)
                return;

            var readings = await response.Content.ReadFromJsonAsync<List<WeatherModel>>(_jsonOptions);

            if (readings is null || readings.Count == 0)
                return;

            foreach (var reading in readings)
                AddReading(reading);
        }

        public async ValueTask DisposeAsync()
        {
            _timer?.Stop();
            _timer?.Dispose();

            if (_weatherSubscriptionId.HasValue)
            {
                SseClient.Unsubscribe(_messageType, _weatherSubscriptionId.Value);
                _weatherSubscriptionId = null;
            }

            Console.WriteLine("Weather component is being disposed (async).");
        }
    }
}
