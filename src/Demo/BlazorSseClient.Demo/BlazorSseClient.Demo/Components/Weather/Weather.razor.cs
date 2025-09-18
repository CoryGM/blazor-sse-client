using System.Text.Json;
using BlazorSseClient.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorSseClient.Demo.Components.Weather
{
    public partial class Weather : ComponentBase, IAsyncDisposable
    {
        [Inject]
        private ISseClient SseClient { get; set; } = null!;

        private readonly List<WeatherModel> readings = [];
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                readings.Clear(); // Optional: clear any server-side data
                var index = SseClient.Subscribe("Weather", AddReading);
            }
        }

        private void AddReading(SseEvent sseEvent)
        {
            if (OperatingSystem.IsBrowser())
            {
                Console.WriteLine($"AddReading() executing in Browser.");
            }
            else
            {
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

                readings.RemoveAll(r => r.City == currentWeather.Value.City);

                foreach (var reading in readings)
                {
                    reading.IsLastReported = false;
                }

                readings.Add(new WeatherModel
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
                    IsLastReported = true
                });

                readings.Sort((a, b) => string.Compare(a.City, b.City, StringComparison.OrdinalIgnoreCase));

            }
            catch (JsonException)
            {
                // Log or handle the error as needed
                return;
            }

            Console.WriteLine($"Before StateHasChanged. readings.Count: {readings.Count}");
            InvokeAsync(StateHasChanged);
            Console.WriteLine("After StateHasChanged");
        }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("Weather component is being disposed (async).");
        }
    }
}
