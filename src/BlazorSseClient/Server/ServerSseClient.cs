using System.Net.ServerSentEvents;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using BlazorSseClient.Services;

namespace BlazorSseClient.Server
{
    public class ServerSseClient : SseClientBase, ISseClient
    {
        private readonly ISseStreamClient _sseStreamClient;
        private readonly ServerSseClientOptions _options;
        private string _currentUrl = String.Empty;
        private CancellationTokenSource? _cts;
        private Task? _listenTask;
        private SseRunState _runState = SseRunState.Stopped;
        private SseConnectionState _connectionState = SseConnectionState.Closed;

        public ServerSseClient(ISseStreamClient sseStreamClient, ServerSseClientOptions options, ILogger<ServerSseClient>? logger)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(sseStreamClient, nameof(sseStreamClient));

            _sseStreamClient = sseStreamClient;
            _options = options ?? new ServerSseClientOptions();
        }

        public SseRunState RunState => _runState;
        public SseConnectionState ConnectionState => _connectionState;

        public Task StartAsync(string? url, bool restartOnDifferentUrl = true)
        {
            var effectiveUrl = GetEffectiveUrl(url, _options.BaseAddress);

            if (String.IsNullOrWhiteSpace(effectiveUrl))
                throw new InvalidOperationException("Cannot start SSE listener. No URL specified and no BaseAddress configured.");

            if (_runState == SseRunState.Started)
            {
                if (restartOnDifferentUrl && !String.Equals(effectiveUrl, _currentUrl, StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogDebug("SSE already started with a different URL; Restarting the Service.");
                    _ = StopAsync();
                }
                else
                {
                    _logger?.LogDebug("SSE already started with the same URL; ignoring StartAsync.");
                    return Task.CompletedTask;
                }
            }

            _logger?.LogInformation("Starting SSE listener for {Url}", effectiveUrl);
            _currentUrl = effectiveUrl;
            _cts = new CancellationTokenSource();

            ChangeRunState(SseRunState.Starting);

            _listenTask = Task.Run(() => ListenLoopAsync(effectiveUrl, _cts.Token));

            ChangeRunState(SseRunState.Started);

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_runState != SseRunState.Started) return;

            _logger?.LogInformation("Stopping SSE listener.");

            ChangeRunState(SseRunState.Stopping);

            try { _cts?.Cancel(); } catch { }

            if (_listenTask is not null)
            {
                try
                {
                    var finished = await Task.WhenAny(_listenTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
                    
                    if (finished != _listenTask)
                        _logger?.LogWarning("SSE listen task did not complete within timeout.");
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Error awaiting listen task.");
                }

                ChangeConnectionState(SseConnectionState.Closed);
            }

            _cts?.Dispose();
            _cts = null;
            _listenTask = null;

            ChangeRunState(SseRunState.Stopped);
        }

        public override async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }

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
                    //await DispatchRunStateChangeAsync(SseClientSource.Server, state).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error dispatching Run State Change from server.");
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
                    //await DispatchConnectionStateChangeAsync(SseClientSource.Server, state).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error dispatching Connection State Change from server.");
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
                    _logger?.LogError(ex, "Error dispatching SSE message from server.");
                }
            });
        }

        private async Task ListenLoopAsync(string url, CancellationToken ct)
        {
            // Reconnect/backoff loop
            var attempt = 0;

            while (!ct.IsCancellationRequested)
            {
                ChangeConnectionState(attempt == 0 ? SseConnectionState.Opening : SseConnectionState.Reopening);

                try
                {
                    using var stream = await _sseStreamClient.GetSseStreamAsync(url, ct).ConfigureAwait(false);

                    ChangeConnectionState(attempt == 0 ? SseConnectionState.Open : SseConnectionState.Reopened);

                    attempt = 0; // reset backoff after a successful connection

                    await foreach (var item in SseParser.Create(stream).EnumerateAsync(ct))
                    {
                        ct.ThrowIfCancellationRequested();

                        string? eventId = null;

                        if (!String.IsNullOrEmpty(item.Data))
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

                        var sseEvent = new SseEvent(item.EventType, item.Data, eventId);
                        _logger?.LogDebug("SSE event received. Id={Id} Type={Type}", sseEvent.Id, sseEvent.EventType);
                        DispatchOnMessage(sseEvent);
                    }

                    // If we exit the foreach without cancellation or exception, treat as end-of-stream
                    _logger?.LogWarning("SSE stream ended unexpectedly; will attempt to reconnect.");
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogDebug("SSE listening cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "SSE listening failed; will attempt to reconnect.");
                }

                // Backoff before reconnect unless stopping
                if (ct.IsCancellationRequested)
                    break;

                attempt++;
                var delayMs = ComputeBackoff(attempt, _options.ReconnectBaseDelayMs, _options.ReconnectMaxDelayMs, _options.ReconnectJitterMs);
                _logger?.LogInformation("Reconnecting to SSE in {Delay} ms (attempt {Attempt}).", delayMs, attempt);

                try
                {
                    await Task.Delay(delayMs, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            ChangeConnectionState(SseConnectionState.Closed);
        }

        private static int ComputeBackoff(int attempt, int baseMs, int maxMs, int jitterMs)
        {
            // exponential backoff with cap and jitter
            try
            {
                var exp = checked(baseMs * (1 << Math.Min(attempt - 1, 16))); // cap shift to avoid overflow
                var capped = Math.Min(exp, maxMs);

                if (jitterMs > 0)
                {
                    var rnd = Random.Shared.Next(0, jitterMs + 1);
                    capped = Math.Min(maxMs, capped + rnd);
                }

                return Math.Max(baseMs, capped);
            }
            catch
            {
                return maxMs; // in overflow scenarios, fall back to max
            }
        }
    }
}

