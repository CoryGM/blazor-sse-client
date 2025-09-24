using System.Text.Json;

using BlazorSseClient.Demo.Api.Queues;
using BlazorSseClient.Demo.Api.SportsScores.Data;

namespace BlazorSseClient.Demo.Api.SportsScores.Background
{
    public class SportsScoreBackgroundService : BackgroundService
    {
        private readonly MessageQueueService _messageQueueService;
        private readonly ISportsScoreService _service;
        private readonly ILogger<SportsScoreBackgroundService> _logger;

        public SportsScoreBackgroundService(
            ISportsScoreService sportsScoreService,
            MessageQueueService service,
            ILogger<SportsScoreBackgroundService> logger)
        {
            ArgumentNullException.ThrowIfNull(sportsScoreService, nameof(sportsScoreService));
            ArgumentNullException.ThrowIfNull(service, nameof(service));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _service = sportsScoreService;
            _messageQueueService = service;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SportsScoresBackgroundService is running.");

            // Simulate sending sports scores every 3 seconds
            while (!stoppingToken.IsCancellationRequested)
            {
                var scoreUpdate = _service.GetRandomScore();

                await _messageQueueService.PublishAsync(scoreUpdate).ConfigureAwait(false);
                
                _logger.LogInformation("Enqueued sports score update: {Message}", JsonSerializer.Serialize(scoreUpdate));
                
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }

            _logger.LogInformation("SportsScoresBackgroundService is stopping.");
        }
    }
}
