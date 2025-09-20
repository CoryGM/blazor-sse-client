using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using BlazorSseClient.Services;

namespace BlazorSseClient.Wasm;

public sealed class WasmSseClient : SseClientBase, ISseClient
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private Task<IJSObjectReference?>? _moduleTask;
    private readonly object _moduleLock = new();
    private readonly HashSet<string> _pendingSubscriptions = new(StringComparer.OrdinalIgnoreCase);

    private DotNetObjectReference<CallbackSink>? _objRef;
    private readonly CallbackSink _sink;
    private SseRunState _runState = SseRunState.Stopped;
    private SseConnectionState _connectionState = SseConnectionState.Closed;
    private bool _disposed = false;
    private string? _currentUrl = null;
    private string? _baseAddress = null;
    private readonly WasmSseClientOptions _options;

    public SseRunState RunState { get => _runState; }
    public SseConnectionState ConnectionState { get => _connectionState; }

    public WasmSseClient(IJSRuntime js, IOptions<WasmSseClientOptions>? options, ILogger<WasmSseClient>? logger = null)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(js, nameof(js));

        _options = options?.Value ?? new WasmSseClientOptions();

        _js = js;
        _baseAddress = options?.Value.BaseAddress;
        _sink = new CallbackSink(this, logger);
        _objRef = DotNetObjectReference.Create(_sink);

        _logger?.LogTrace("WasmSseClient constructed. Default Url: {BaseAddress}; AutoStart: {AutoStart}",
            _baseAddress ?? "None", options?.Value.AutoStart);

        // Start module load immediately (do not block constructor).
        // Subscribers / starters will await or enqueue as needed.
        _moduleTask = EnsureModuleLoadedAsync();

        // If AutoStart is requested, kick off StartAsync in background. StartAsync will await the module task.
        if (options?.Value.AutoStart == true && !String.IsNullOrWhiteSpace(_baseAddress))
        {
            _ = StartAsync(_baseAddress!, false);
        }
    }

    /// <summary>
    /// Starts the SSE connection to the specified URL.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="restartOnDifferentUrl"></param>
    /// <returns></returns>
    public async Task StartAsync(string? url, bool restartOnDifferentUrl = true)
    {
        // Ensure module is loaded before invoking startSse.
        await EnsureModuleLoadedAsync().ConfigureAwait(false);

        var effectiveUrl = GetEffectiveUrl(url, _options.BaseAddress, _options.QueryParameters);

        if (String.IsNullOrWhiteSpace(effectiveUrl))
            throw new ArgumentException("URL is required.", nameof(url));

        if (_runState == SseRunState.Started)
        {
            if (!restartOnDifferentUrl || String.Equals(effectiveUrl, _currentUrl, StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogDebug("SSE already started; ignoring StartAsync.");
                return;
            }

            await InternalStopAsync().ConfigureAwait(false);
        }

        _currentUrl = effectiveUrl;

        _logger?.LogInformation("Starting SSE connection to {CurrentUrl}", _currentUrl);

        var payload = new
        {
            reconnectBaseDelayMs = _options.ReconnectBaseDelayMs,
            reconnectMaxDelayMs = _options.ReconnectMaxDelayMs,
            reconnectJitterMs = _options.ReconnectJitterMs,
            useCredentials = _options.UseCredentials
        };

        // _module is guaranteed to be set by EnsureModuleLoadedAsync
        if (_module is null)
        {
            // Shouldn't happen, but guard defensively.
            throw new InvalidOperationException("JS module not available.");
        }

        await _module.InvokeVoidAsync("startSse", _currentUrl, _objRef, payload).ConfigureAwait(false);
    }

    public override Guid Subscribe(string eventType, Action<SseEvent> handler, CancellationToken cancellationToken = default)
    {
        var id = base.Subscribe(eventType, handler, cancellationToken);

        // Fire-and-forget registration with the JS module. If the module isn't ready yet,
        // queue the subscription and it will be attached when module load completes.
        RegisterSubscriptionInJs(eventType);

        return id;
    }

    public override Guid Subscribe(string eventType, Func<SseEvent, ValueTask> handler, CancellationToken cancellationToken = default)
    {
        var id = base.Subscribe(eventType, handler, cancellationToken);

        // Fire-and-forget registration with the JS module. If the module isn't ready yet,
        // queue the subscription and it will be attached when module load completes.
        RegisterSubscriptionInJs(eventType);

        return id;
    }

    public override void Unsubscribe(string eventType, Guid id)
    {
        // Remove pending subscription if it wasn't sent to JS yet
        lock (_moduleLock)
        {
            if (_pendingSubscriptions.Remove(eventType))
            {
                // we removed from pending; nothing to call on JS
            }
            else
            {
                // If module present, inform JS to unsubscribe
                if (_module is not null)
                {
                    try
                    {
                        _ = _module.InvokeVoidAsync("unsubscribe", eventType);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "Ignoring unsubscribe error.");
                    }
                }
            }
        }

        base.Unsubscribe(eventType, id);
    }

    /// <summary>
    /// Stops the client from listening for events from the server.
    /// </summary>
    /// <returns></returns>
    public async Task StopAsync()
    {
        await InternalStopAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Ensure the module load is started and return the module (or throw if import fails).
    /// This uses a single Task to guard concurrent import attempts and attaches any pending subscriptions when complete.
    /// </summary>
    private Task<IJSObjectReference?> EnsureModuleLoadedAsync()
    {
        lock (_moduleLock)
        {
            if (_moduleTask is not null)
                return _moduleTask;

            _moduleTask = LoadAndInitializeModuleAsync();
            return _moduleTask;
        }
    }

    private async Task<IJSObjectReference?> LoadAndInitializeModuleAsync()
    {
        try
        {
            _logger?.LogTrace("Importing JS module.");

            var mod = await _js.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorSseClient/js/sse-client.js").AsTask().ConfigureAwait(false);

            lock (_moduleLock)
            {
                _module = mod;

                // Attach any queued subscriptions
                if (_pendingSubscriptions.Count > 0)
                {
                    foreach (var evt in _pendingSubscriptions)
                    {
                        try
                        {
                            _ = _module.InvokeVoidAsync("subscribe", evt);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogDebug(ex, "Failed to attach pending subscription {Event}", evt);
                        }
                    }

                    _pendingSubscriptions.Clear();
                }
            }

            _logger?.LogTrace("JS module imported.");
            return mod;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "JS module import failed.");
            // propagate so callers (StartAsync) see the failure
            throw;
        }
    }

    private void RegisterSubscriptionInJs(string eventType)
    {
        if (String.IsNullOrWhiteSpace(eventType))
            return;

        lock (_moduleLock)
        {
            if (_module is not null)
            {
                try
                {
                    _ = _module.InvokeVoidAsync("subscribe", eventType);
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "subscribe failed for {Event}", eventType);
                }
            }
            else
            {
                // queue for later attach
                _pendingSubscriptions.Add(eventType);

                // ensure module load has been started
                if (_moduleTask is null)
                {
                    _moduleTask = EnsureModuleLoadedAsync();
                }
            }
        }
    }

    private async Task InternalStopAsync()
    {
        if (_module is not null)
        {
            try
            {
                _logger?.LogInformation("Stopping SSE (URL {Url})", _currentUrl);
                await _module.InvokeVoidAsync("stopSse").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Ignoring stopSse error during shutdown.");
            }
        }

        _currentUrl = null;
        _logger?.LogTrace("InternalStopAsync: state cleared.");
    }

    private void DispatchRunStateChange(SseRunState state)
    {
        _runState = state;

        _logger?.LogDebug("WASM SSE run state: {State}", RunState);

        _ = Task.Run(async () =>
        {
            try
            {
                //await DispatchRunStateChangeAsync(SseClientSource.Wasm, state).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error dispatching Run State Change from WASM.");
            }
        });
    }

    private void DispatchConnectionStateChange(SseConnectionState state)
    {
        _connectionState = state == SseConnectionState.Reopened ? SseConnectionState.Open :
                                                                  state;

        _logger?.LogDebug("WASM SSE connection state: {State}", ConnectionState);

        _ = Task.Run(async () =>
        {
            try
            {
                //await DispatchConnectionStateChangeAsync(SseClientSource.Wasm, state).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error dispatching Connection State Change from WASM.");
            }
        });
    }

    private void DispatchOnMessage(SseEvent sseEvent)
    {
        _logger?.LogInformation("WASM SSE message: Id={Id} Type={Type}", sseEvent.Id, sseEvent.EventType);

        _ = Task.Run(async () =>
        {
            try
            {
                await DispatchOnMessageAsync(SseClientSource.Wasm, sseEvent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error dispatching SSE message from WASM.");
            }
        });
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await InternalStopAsync().ConfigureAwait(false);

        // Wait for module task to finish if it is running, then dispose
        try
        {
            if (_moduleTask is not null)
            {
                var m = await _moduleTask.ConfigureAwait(false);
                if (m is not null)
                    await m.DisposeAsync().ConfigureAwait(false);
            }
            else if (_module is not null)
            {
                await _module.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error disposing module.");
        }

        _module = null;
        _moduleTask = null;
        _objRef?.Dispose();
        _objRef = null;

        _disposed = true;

        await base.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Proxy class for JS -> .NET callbacks. This class is instantiated
    /// by the SseClient and passed to JS as a DotNetObjectReference. This is
    /// done to prevent the JSInvokable methods from appearing on the SseClient's
    /// public signature. 
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="logger"></param>
    private sealed class CallbackSink(WasmSseClient parent, ILogger<WasmSseClient>? logger)
    {
        private readonly WasmSseClient _parent = parent;
        private readonly ILogger<WasmSseClient>? _logger = logger;

        /// <summary>
        /// Handler for run state changes from JS.
        /// </summary>
        /// <param name="runState"></param>
        [JSInvokable]
        public void OnSseRunStateChange(int? runState)
        {
            if (runState is null)
                return;

            if (Enum.TryParse<SseRunState>($"{runState}", true, out var state))
            {
                _logger?.LogInformation("SSE run state changed to {State}", state);

                //  Dispatch event to listeners here
                _parent.DispatchRunStateChange(state);
            }
            else
            {
                _logger?.LogWarning("Received unknown SSE run state: {State}", runState);
            }
        }

        /// <summary>
        /// Handler for connection state changes from JS.
        /// </summary>
        /// <param name="connectionState"></param>
        [JSInvokable]
        public void OnSseConnectionStateChange(int? connectionState)
        {
            if (connectionState is null)
                return;

            if (Enum.TryParse<SseConnectionState>($"{connectionState}", true, out var state))
            {
                _logger?.LogInformation("SSE connection state changed to {State}", state);

                //  Dispatch event to listeners here
                _parent.DispatchConnectionStateChange(state);
            }
            else
            {
                _logger?.LogWarning("Received unknown SSE connection state: {State}", connectionState);
            }
        }

        /// <summary>
        /// Handler for incoming SSE messages from JS.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="data"></param>
        /// <param name="id"></param>
        [JSInvokable]
        public void OnSseMessage(string eventType, string? data, string? id)
        {
            if (String.IsNullOrWhiteSpace(eventType))
                return;

            var sseEvent = new SseEvent(eventType, data ?? String.Empty, id);

            var preview = data?.Length > 64 ? data[..64] + "…" : data;
            _logger?.LogTrace("Dispatched {Event} (Id={Id}, Data='{Preview}')", eventType, id ?? "null", preview);

            _parent.DispatchOnMessage(sseEvent);
        }
    }
}
