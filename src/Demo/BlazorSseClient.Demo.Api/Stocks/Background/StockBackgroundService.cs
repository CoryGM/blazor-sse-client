using System.Text.Json;

using BlazorSseClient.Demo.Api.Queues;
using BlazorSseClient.Demo.Api.Stocks.Data;

namespace BlazorSseClient.Demo.Api.Stock.Background
{
    public class StockBackgroundService : BackgroundService
    {
        private readonly MessageQueueService _messageQueueService;
        private readonly IStockService _service;
        private readonly ILogger<StockBackgroundService> _logger;

        public StockBackgroundService(
            IStockService service,
            MessageQueueService messageQueueService,
            ILogger<StockBackgroundService> logger)
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
            _logger.LogInformation("StockBackgroundService is running.");

            // Simulate sending sports scores every 5 seconds
            while (!stoppingToken.IsCancellationRequested)
            {
                var stockUpdate = _service.GetNextQuote();

                await _messageQueueService.PublishAsync(stockUpdate).ConfigureAwait(false);
                
                _logger.LogInformation("Enqueued stock update: {Message}", JsonSerializer.Serialize(stockUpdate));
                
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }

            _logger.LogInformation("StockBackgroundService is stopping.");
        }
    }
}
