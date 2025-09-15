using System.Text.Json;

using Microsoft.Extensions.Logging;

using BlazorSseClient.Services;

namespace BlazorSseClient.Server
{
    public class ServerSseClient : ISseClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ServerSseClient> _logger;
        private string _currentUrl = string.Empty;

        public ServerSseClient(IHttpClientFactory httpClientFactory, ILogger<ServerSseClient> logger)
        {
            ArgumentNullException.ThrowIfNull(httpClientFactory, nameof(httpClientFactory));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _httpClientFactory = httpClientFactory;
            _logger = logger;

            // Simulate receiving SSE events
            _ = SimulateSseEvents();
        }
        public async Task StartAsync(string url, bool restartOnDifferentUrl = true)
        {
            Console.WriteLine($"ServerSseClient: StartAsync called with URL: {url}");
        }

        public async Task StopAsync()
        {
            Console.WriteLine("ServerSseClient: StopAsync called");
        }

        public async ValueTask DisposeAsync()
        {
            return;
        }


        private async Task SimulateSseEvents()
        {
            //using var client = new HttpClient();
            //using var stream = await client.GetStreamAsync(_currentUrl);
            //await foreach (var item in SseParser.Create(stream, (eventType, bytes) => eventType switch
            //{
            //    "WeatherForecast" => JsonSerializer.Deserialize<WeatherForecast>(bytes),
            //    "SportScore" => JsonSerializer.Deserialize<SportScore>(bytes) as object,
            //    _ => null
            //}).EnumerateAsync())
            //{
            //    switch (item.Data)
            //    {
            //        case WeatherForecast weatherForecast:
            //            Console.WriteLine($"Date: {weatherForecast.Date}, Temperature (in C): {weatherForecast.TemperatureC}, Summary: {weatherForecast.Summary}");
            //            break;
            //        case SportScore sportScore:
            //            Console.WriteLine($"Team 1 vs Team 2 {sportScore.Team1Score}:{sportScore.Team2Score}");
            //            break;
            //        default:
            //            Console.WriteLine("Couldn't deserialize the response");
            //            break;
            //    }
            //}
        }

    }
}
