using System.Net.ServerSentEvents;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using BlazorSseClient.Services;

namespace BlazorSseClient.Server
{
    public class ServerSseClient : SseClientBase, ISseClient, IAsyncDisposable
    {
        private readonly ISseStreamClient _sseStreamClient;
        private readonly ILogger<ServerSseClient> _logger;
        private string _currentUrl = string.Empty;
        private CancellationTokenSource? _cts;
        private Task? _listenTask;
        private SseRunState _runState = SseRunState.Stopped;
        private SseConnectionState _connectionState = SseConnectionState.Closed;

        public ServerSseClient(ISseStreamClient sseStreamClient, ILogger<ServerSseClient> logger)
        {
            ArgumentNullException.ThrowIfNull(sseStreamClient, nameof(sseStreamClient));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _sseStreamClient = sseStreamClient;
            _logger = logger;
        }

        public SseRunState RunState => _runState;
        public SseConnectionState ConnectionState => _connectionState;

        public Task StartAsync(string? url, bool restartOnDifferentUrl = true)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL is required.", nameof(url));

            if (_runState == SseRunState.Started)
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
            ChangeRunState(SseRunState.Starting);

            _listenTask = Task.Run(() => ListenLoopAsync(url, _cts.Token));

            ChangeRunState(SseRunState.Started);

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_runState != SseRunState.Started) return;

            _logger.LogInformation("Stopping SSE listener.");

            ChangeRunState(SseRunState.Stopping);

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

                ChangeConnectionState(SseConnectionState.Closed);
            }

            _cts?.Dispose();
            _cts = null;
            _listenTask = null;

            ChangeRunState(SseRunState.Stopped);
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

        private void ChangeRunState(SseRunState state)
        {
            _runState = state;
            DispatchRunStateChange(state);
        }

        private void DispatchRunStateChange(SseRunState state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await DispatchRunStateChangeAsync(SseClientSource.Server, state).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error dispatching Run State Change from server.");
                }
            });
        }

        private void ChangeConnectionState(SseConnectionState state)
        {
            _connectionState = state;
            DispatchConnectionStateChange(state);
        }   

        private void DispatchConnectionStateChange(SseConnectionState state)
        {
            _connectionState = state == SseConnectionState.Reopened ? SseConnectionState.Open :
                                                                      state;

            _ = Task.Run(async () =>
            {
                try
                {
                    await DispatchConnectionStateChangeAsync(SseClientSource.Server, state).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error dispatching Connection State Change from server.");
                }
            });
        }

        private void DispatchOnMessage(SseEvent sseEvent)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await DispatchOnMessageAsync(SseClientSource.Server, sseEvent).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error dispatching SSE message from server.");
                }
            });
        }

        private async Task ListenLoopAsync(string url, CancellationToken ct)
        {
            ChangeConnectionState(SseConnectionState.Opening);

            try
            {
                using var stream = await _sseStreamClient.GetSseStreamAsync(url, ct).ConfigureAwait(false);

                ChangeConnectionState(SseConnectionState.Open);

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
                    DispatchOnMessage(sseEvent);
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

