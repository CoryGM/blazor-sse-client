using System.Text.Json;
using System.Threading.Channels;

namespace BlazorSseClient.Demo.Api.Queues
{
    public class MessageQueueService : IAsyncDisposable
    {
        private readonly Channel<QueueMessage?> _channel;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public MessageQueueService()
        {
            // Bounded channel with a capacity of 100 messages
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            };

            _channel = Channel.CreateBounded<QueueMessage?>(options);
        }

        public async ValueTask EnqueueAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            if (message is null)
                return;

            var queueMessage = new QueueMessage
            {
                Type = typeof(T).Name,
                Version = 1,
                Payload = JsonSerializer.Serialize(message, _jsonOptions)
            };

            await _channel.Writer.WriteAsync(queueMessage, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Await the next message (throws OperationCanceledException if cancelled).
        /// </summary>
        public ValueTask<QueueMessage?> DequeueAsync(CancellationToken cancellationToken) =>
            _channel.Reader.ReadAsync(cancellationToken);

        /// <summary>
        /// Stream all messages until completion or cancellation.
        /// </summary>
        public IAsyncEnumerable<QueueMessage?> ReadAllAsync(CancellationToken cancellationToken = default) =>
            _channel.Reader.ReadAllAsync(cancellationToken);

        /// <summary>
        /// Signal no more messages will be written. Readers drain remaining items.
        /// </summary>
        public bool TryComplete(Exception? error = null) =>
            _channel.Writer.TryComplete(error);

        public ValueTask DisposeAsync()
        {
            TryComplete();
            return ValueTask.CompletedTask;
        }
    }
}
