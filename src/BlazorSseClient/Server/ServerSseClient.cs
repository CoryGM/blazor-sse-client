using System.Net.ServerSentEvents;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using BlazorSseClient.Services;

namespace BlazorSseClient.Server
{
    public class ServerSseClient : ISseClient, IAsyncDisposable
    {
        private readonly ISseStreamClient _sseStreamClient;
        private readonly ILogger<ServerSseClient> _logger;
        private string _currentUrl = string.Empty;
        private CancellationTokenSource? _cts;
        private Task? _listenTask;
        private volatile bool _started;

        public ServerSseClient(ISseStreamClient sseStreamClient, ILogger<ServerSseClient> logger)
        {
            _sseStreamClient = sseStreamClient ?? throw new ArgumentNullException(nameof(sseStreamClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(string? url, bool restartOnDifferentUrl = true)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL is required.", nameof(url));

            if (_started)
            {
                if (!restartOnDifferentUrl || string.Equals(url, _currentUrl, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("SSE already started; ignoring StartAsync.");

                    return Task.CompletedTask;
                }

                _ = StopAsync();
            }

            _logger.LogInformation("Starting SSE listener for {Url}", url);
            _currentUrl = url;
            _cts = new CancellationTokenSource();
            _started = true;

            _listenTask = Task.Run(() => ListenLoopAsync(url, _cts.Token));

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (!_started) return;

            _logger.LogInformation("Stopping SSE listener.");
            _started = false;

            try { _cts?.Cancel(); } catch { }

            if (_listenTask is not null)
            {
                try
                {
                    var finished = await Task.WhenAny(_listenTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
                    
                    if (finished != _listenTask)
                        _logger.LogWarning("SSE listen task did not complete within timeout.");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error awaiting listen task.");
                }
            }

            _cts?.Dispose();
            _cts = null;
            _listenTask = null;
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

        private Task DispatchOnMessageAsync(SseEvent e)
        {
            _logger.LogInformation("SSE Message: {Id} {Type} : {Data}", e.Id, e.EventType, e.Data);
            return Task.CompletedTask;
        }

        private async Task ListenLoopAsync(string url, CancellationToken ct)
        {
            try
            {
                using var stream = await _sseStreamClient.GetSseStreamAsync(url, ct).ConfigureAwait(false);

                await foreach (var item in SseParser.Create(stream).EnumerateAsync(ct))
                {
                    ct.ThrowIfCancellationRequested();

                    string? eventId = null;
                    if (!string.IsNullOrEmpty(item.Data))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(item.Data);
                            if (doc.RootElement.TryGetProperty("LastEventId", out var p1) && p1.ValueKind == JsonValueKind.String)
                                eventId = p1.GetString();

                            else if (doc.RootElement.TryGetProperty("Id", out var p2) && p2.ValueKind == JsonValueKind.String)
                                eventId = p2.GetString();

                            else if (doc.RootElement.TryGetProperty("EventId", out var p3) && p3.ValueKind == JsonValueKind.String)
                                eventId = p3.GetString();
                        }
                        catch (JsonException) { /* ignore */ }
                    }

                    var sseEvent = new SseEvent { EventType = item.EventType, Data = item.Data, Id = eventId };
                    _logger.LogDebug("SSE event received. Id={Id} Type={Type}", sseEvent.Id, sseEvent.EventType);
                    await DispatchOnMessageAsync(sseEvent).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("SSE listening cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSE listening failed.");
            }
        }
    }
}

