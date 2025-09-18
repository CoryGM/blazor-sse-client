using System.Text.Json;

using BlazorSseClient.Demo.Api.Queues;
using BlazorSseClient.Demo.Api.Weather.Data;

namespace BlazorSseClient.Demo.Api.Weather.Background
{
    public class WeatherBackgroundService : BackgroundService
    {
        private readonly MessageQueueService _messageQueueService;
        private readonly IWeatherService _service;
        private readonly ILogger<WeatherBackgroundService> _logger;

        public WeatherBackgroundService(
            IWeatherService service,
            MessageQueueService messageQueueService,
            ILogger<WeatherBackgroundService> logger)
        {
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(messageQueueService, nameof(messageQueueService));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _service = service;
            _messageQueueService = messageQueueService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WeatherBackgroundService is running.");

            // Simulate sending sports scores every 5 seconds
            while (!stoppingToken.IsCancellationRequested)
            {
                var weatherUpdate = await _service.GetRandomCurrentWeatherAsync().ConfigureAwait(false);

                await _messageQueueService.PublishAsync("Weather", weatherUpdate).ConfigureAwait(false);
                
                _logger.LogInformation("Enqueued weather update: {Message}", JsonSerializer.Serialize(weatherUpdate));
                
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("WeatherBackgroundService is stopping.");
        }
    }
}
