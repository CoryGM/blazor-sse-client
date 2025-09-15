using BlazorSseClient.Services;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Net.ServerSentEvents;
using System.Text.Json;

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
        }

        public async Task StartAsync(string url, bool restartOnDifferentUrl = true)
        {
            Console.WriteLine($"ServerSseClient: StartAsync called with URL: {url}");
            await ListenAsync().ConfigureAwait(false);
        }

        public async Task StopAsync()
        {
            Console.WriteLine("ServerSseClient: StopAsync called");
        }

        public async ValueTask DisposeAsync()
        {
            return;
        }

        private void DispatchRunStateChange(SseRunState state)
        {
            //_runState = state;

            ////  Dispatch events based on state
            //Console.WriteLine($"Run state changed to {_runState}");
        }

        private void DispatchConnectionStateChange(SseConnectionState state)
        {
            //_connectionState = state == SseConnectionState.Reopened ? SseConnectionState.Open :
            //                                                          state;

            //Console.WriteLine($"Connection state changed to {_connectionState}");
            ////  Dispatch events based on state
        }

        private async Task DispatchOnMessageAsync(SseEvent? sseMessage)
        {
            Console.WriteLine($"Received SSE Message: {sseMessage?.Id} - {sseMessage?.EventType} - {sseMessage?.Data}");
        }

        private async Task ListenAsync()
        {
            using var client = new HttpClient();

            if (string.IsNullOrEmpty(_currentUrl))
                return;

            using var stream = await client.GetStreamAsync(_currentUrl);

            await foreach (var item in SseParser.Create(stream).EnumerateAsync())
            {
                // try common property names first, fall back to reflection if necessary
                string? eventId = null;

                // common property names used by various SSE libs
                eventId ??= (item as dynamic) is not null ? (item as dynamic).LastEventId as string : null;
                eventId ??= (item as dynamic) is not null ? (item as dynamic).Id as string : null;
                eventId ??= (item as dynamic) is not null ? (item as dynamic).EventId as string : null;

                if (eventId is null)
                {
                    // reflection fallback (safe if you don't know exact property)
                    var p = item.GetType().GetProperty("LastEventId")
                            ?? item.GetType().GetProperty("Id")
                            ?? item.GetType().GetProperty("EventId");

                    eventId = p?.GetValue(item) as string;
                }

                var sseEvent = new SseEvent
                {
                    EventType = item.EventType,
                    Data = item.Data,
                    Id = eventId
                };

                Console.WriteLine($"SSE id={eventId} event={item.EventType}");

                await DispatchOnMessageAsync(sseEvent).ConfigureAwait(false);
            }
        }

    }
}
