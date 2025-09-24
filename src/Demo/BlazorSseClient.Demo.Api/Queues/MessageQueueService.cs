using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace BlazorSseClient.Demo.Api.Queues
{
    public class MessageQueueService : IAsyncDisposable
    {
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private readonly ConcurrentDictionary<Guid, Channel<QueueMessage>> _subscribers = [];

        public MessageQueueService()
        {
        }

        public async ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        {
            var messageType = typeof(T).Name;
            await PublishAsync(messageType, message, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask PublishAsync<T>(string messageType, T message, CancellationToken cancellationToken = default)
        {
            if (message is null)
                return;

            var queueMessage = new QueueMessage
            {
                Type = messageType,
                Version = 1,
                Payload = JsonSerializer.Serialize(message, _jsonOptions)
            };

            foreach (var channel in _subscribers.Values)
                await channel.Writer.WriteAsync(queueMessage, cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<QueueMessage> Subscribe([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<QueueMessage>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            var id = Guid.NewGuid();

            _subscribers.TryAdd(id, channel);

            try
            {
                while (await channel.Reader.WaitToReadAsync(cancellationToken))
                {
                    while (channel.Reader.TryRead(out var message))
                    {
                        yield return message;
                    }
                }
            }
            finally
            {
                _subscribers.TryRemove(id, out _);
                channel.Writer.TryComplete();
            }
        }

        public ValueTask DisposeAsync()
        {
            foreach (var channel in _subscribers.Values)
                channel.Writer.TryComplete();

            return ValueTask.CompletedTask;
        }
    }
}
