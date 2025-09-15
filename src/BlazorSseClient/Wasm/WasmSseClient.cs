using BlazorSseClient.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorSseClient.Wasm;

/// <summary>
/// Browser (EventSource via JS) SSE client using weak-reference subscriptions.
/// </summary>
public sealed class WasmSseClient : ISseClient, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<CallbackSink>? _objRef;
    private readonly CallbackSink _sink;
    private readonly ILogger<WasmSseClient>? _logger;
    private SseRunState _runState = SseRunState.Stopped;
    private SseConnectionState _connectionState = SseConnectionState.Closed;
    private bool _disposed = false;
    private string? _currentUrl = null;
    private string? _defaultUrl = null;

    public SseRunState RunState { get => _runState; }
    public SseConnectionState ConnectionState { get => _connectionState; }

    public WasmSseClient(IJSRuntime js, string? defaultUrl = null, bool? autoStart = false, ILogger<WasmSseClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(js, nameof(js));

        _js = js;
        _defaultUrl = defaultUrl;
        _logger = logger;
        _sink = new CallbackSink(this, logger);
        _objRef = DotNetObjectReference.Create(_sink);

        _logger?.LogTrace($"WasmSseClient constructed. Default Url: {_defaultUrl ?? "None"}; AutoStart: {autoStart}");

        if (autoStart == true && !String.IsNullOrWhiteSpace(_defaultUrl))
        {
            _ = StartAsync(_defaultUrl!, false);
        }
    }

    /// <summary>
    /// Starts the SSE connection to the specified URL.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="restartOnDifferentUrl"></param>
    /// <returns></returns>
    public async Task StartAsync(string url, bool restartOnDifferentUrl = true)
    {
        _currentUrl = url;

        if (_module is null)
        {
            _logger?.LogTrace("Importing JS module.");

            _module = await _js.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorSseClient/js/sse-client.js").ConfigureAwait(false);
        }

        _logger?.LogInformation("Starting SSE connection to {Url}", url);

        await _module.InvokeVoidAsync("startSse", url, _objRef).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the client from listening for events from the server.
    /// </summary>
    /// <returns></returns>
    public async Task StopAsync()
    {
        await InternalStopAsync().ConfigureAwait(false);
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

        //  Dispatch events based on state
        Console.WriteLine($"Run state changed to {_runState}");
    }

    private void DispatchConnectionStateChange(SseConnectionState state)
    {
        _connectionState = state == SseConnectionState.Reopened ? SseConnectionState.Open :
                                                                  state;

        Console.WriteLine($"Connection state changed to {_connectionState}");
        //  Dispatch events based on state
    }

    private void DispatchOnMessage(SseEvent? sseMessage)
    {
        Console.WriteLine($"Received SSE Message: {sseMessage?.EventType} - {sseMessage?.Data}");
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, nameof(WasmSseClient));

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await InternalStopAsync().ConfigureAwait(false);

        if (_module is not null)
            await _module.DisposeAsync().ConfigureAwait(false);

        _module = null;
        _objRef?.Dispose();
        _objRef = null;

        _disposed = true;

        _logger?.LogTrace("SseClient disposed.");
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
            if (string.IsNullOrWhiteSpace(eventType))
                return;

            var sseEvent = new SseEvent(eventType, data ?? string.Empty, id);

            var preview = data?.Length > 64 ? data[..64] + "…" : data;
            _logger?.LogTrace("Dispatched {Event} (Id={Id}, Data='{Preview}')", eventType, id ?? "null", preview);

            _parent.DispatchOnMessage(sseEvent);
        }
    }
}

    // Lifecycle ----------------------------------------------------------------

    //public async Task StartAsync(string url, bool restartOnDifferentUrl = true)
    //{
    //    ThrowIfDisposed();
    //    await _lifecycleGate.WaitAsync().ConfigureAwait(false);
    //    try
    //    {
    //        if (_isStarted && string.Equals(_currentUrl, url, StringComparison.Ordinal))
    //        {
    //            _logger?.LogDebug("StartAsync ignored (already started): {Url}", url);
    //            return;
    //        }

    //        if (_isStarted && !string.Equals(_currentUrl, url, StringComparison.Ordinal))
    //        {
    //            if (!restartOnDifferentUrl)
    //            {
    //                _logger?.LogDebug("StartAsync new URL {New} while running {Old} (restart disabled).", url, _currentUrl);
    //                return;
    //            }
    //            _logger?.LogInformation("Restarting SSE from {Old} to {New}", _currentUrl, url);
    //            await InternalStopAsync().ConfigureAwait(false);
    //        }

    //        if (_module is null)
    //        {
    //            _logger?.LogTrace("Importing JS module.");
    //            _module = await _js.InvokeAsync<IJSObjectReference>(
    //                "import", "./_content/BlazorSseClient/js/sse-client.js").ConfigureAwait(false);
    //        }

    //        _currentUrl = url;
    //        _logger?.LogInformation("Starting SSE connection to {Url}", url);
    //        await _module.InvokeVoidAsync("startSse", url, _objRef).ConfigureAwait(false);
    //    }
    //    finally
    //    {
    //        _lifecycleGate.Release();
    //    }
    //}

    //public async Task StopAsync()
    //{
    //    ThrowIfDisposed();
    //    await _lifecycleGate.WaitAsync().ConfigureAwait(false);
    //    try
    //    {
    //        await InternalStopAsync().ConfigureAwait(false);
    //    }
    //    finally
    //    {
    //        _lifecycleGate.Release();
    //    }
    //}

    //private async Task InternalStopAsync()
    //{
    //    if (_module is not null && _isStarted)
    //    {
    //        try
    //        {
    //            _logger?.LogInformation("Stopping SSE (URL {Url})", _currentUrl);
    //            await _module.InvokeVoidAsync("stopSse").ConfigureAwait(false);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger?.LogDebug(ex, "Ignoring stopSse error during shutdown.");
    //        }
    //    }
    //    _isStarted = false;
    //    _isConnected = false;
    //    _currentUrl = null;
    //    _logger?.LogTrace("InternalStopAsync: state cleared.");
    //}

    //// Subscription API (weak) --------------------------------------------------

    //public Guid Subscribe(string eventType, Action<SseEvent> callback)
    //{
    //    if (string.IsNullOrWhiteSpace(eventType))
    //        throw new ArgumentException("Event type must be non-empty.", nameof(eventType));
    //    ArgumentNullException.ThrowIfNull(callback);

    //    var id = Guid.NewGuid();
    //    var bucket = _typedListeners.GetOrAdd(eventType, _ => new());
    //    bucket[id] = new WeakReference<Action<SseEvent>>(callback);
    //    _logger?.LogTrace("Subscribed typed listener {Id} -> {Event}", id, eventType);
    //    return id;
    //}

    //public void Unsubscribe(string eventType, Guid id)
    //{
    //    if (_typedListeners.TryGetValue(eventType, out var bucket))
    //    {
    //        bucket.TryRemove(id, out _);
    //        if (bucket.IsEmpty)
    //            _typedListeners.TryRemove(eventType, out _);
    //        _logger?.LogTrace("Unsubscribed typed listener {Id} from {Event}", id, eventType);
    //    }
    //}

    //public Guid SubscribeAll(Action<SseEvent> callback)
    //{
    //    ArgumentNullException.ThrowIfNull(callback);
    //    var id = Guid.NewGuid();
    //    _allEventListeners[id] = new WeakReference<Action<SseEvent>>(callback);
    //    _logger?.LogTrace("Subscribed all-events listener {Id}", id);
    //    return id;
    //}

    //public void UnsubscribeAll(Guid id)
    //{
    //    if (_allEventListeners.TryRemove(id, out _))
    //        _logger?.LogTrace("Unsubscribed all-events listener {Id}", id);
    //}

    //public Guid SubscribeReconnectAttempt(Action<int> callback)
    //{
    //    ArgumentNullException.ThrowIfNull(callback);
    //    var id = Guid.NewGuid();
    //    _reconnectAttemptListeners[id] = new WeakReference<Action<int>>(callback);
    //    _logger?.LogTrace("Subscribed reconnect-attempt listener {Id}", id);
    //    return id;
    //}

    //public void UnsubscribeReconnectAttempt(Guid id)
    //{
    //    _reconnectAttemptListeners.TryRemove(id, out _);
    //}

    //public Guid SubscribeReconnect(Action<int> callback)
    //{
    //    ArgumentNullException.ThrowIfNull(callback);
    //    var id = Guid.NewGuid();
    //    _reconnectSuccessListeners[id] = new WeakReference<Action<int>>(callback);
    //    _logger?.LogTrace("Subscribed reconnect-success listener {Id}", id);
    //    return id;
    //}

    //public void UnsubscribeReconnect(Guid id)
    //{
    //    _reconnectSuccessListeners.TryRemove(id, out _);
    //}

    // JS -> .NET callbacks ------------------------------------------------------

    //[JSInvokable] public void OnSseStart() { _isStarted = true; _logger?.LogDebug("SSE start signal."); }
    //[JSInvokable] public void OnSseStop() { _isStarted = false; _isConnected = false; _logger?.LogDebug("SSE stop signal."); }
    //[JSInvokable] public void OnSseConnect() { _isConnected = true; _logger?.LogInformation("SSE connected."); }
    //[JSInvokable] public void OnSseError() { _isConnected = false; _logger?.LogWarning("SSE error (disconnected)."); }

    //[JSInvokable]
    //public void OnSseReconnectAttempt(int attempt)
    //{
    //    _isConnected = false;
    //    _logger?.LogDebug("Reconnect attempt #{Attempt}", attempt);
    //    DispatchWeakInt(_reconnectAttemptListeners, attempt);
    //}

    //[JSInvokable]
    //public void OnSseReconnect(int attempts)
    //{
    //    _isConnected = true;
    //    _logger?.LogInformation("Reconnected after {Attempts} attempt(s).", attempts);
    //    DispatchWeakInt(_reconnectSuccessListeners, attempts);
    //}

    //[JSInvokable]
    //public void OnSseMessage(string eventType, string data, string? id)
    //{
    //    if (string.IsNullOrWhiteSpace(eventType))
    //        return;

    //    var evt = new SseEvent(eventType, data, id);

    //    // All-event listeners
    //    if (!_allEventListeners.IsEmpty)
    //        DispatchWeakEventCollection(_allEventListeners, evt, isAll: true);

    //    // Typed listeners
    //    if (_typedListeners.TryGetValue(eventType, out var bucket) && !bucket.IsEmpty)
    //        DispatchWeakEventBucket(eventType, bucket, evt);

    //    if (_logger?.IsEnabled(LogLevel.Trace) == true)
    //    {
    //        var preview = data.Length > 64 ? data[..64] + "…" : data;
    //        _logger.LogTrace("Dispatched {Event} (Id={Id}, Data='{Preview}')", eventType, id ?? "null", preview);
    //    }
    //}

    // Dispatch helpers ---------------------------------------------------------

    //    private void DispatchWeakEventCollection(
    //        ConcurrentDictionary<Guid, WeakReference<Action<SseEvent>>> dict,
    //        SseEvent evt,
    //        bool isAll)
    //    {
    //        List<Guid>? dead = null;
    //        foreach (var kv in dict)
    //        {
    //            if (kv.Value.TryGetTarget(out var cb))
    //            {
    //                try { cb(evt); } catch (Exception ex) { _logger?.LogDebug(ex, "All-event listener threw."); }
    //            }
    //            else
    //            {
    //                (dead ??= new()).Add(kv.Key);
    //            }
    //        }
    //        if (dead is not null)
    //        {
    //            foreach (var d in dead) dict.TryRemove(d, out _);
    //            _logger?.LogTrace("Pruned {Count} dead {Scope} listeners.", dead.Count, isAll ? "all-event" : "typed");
    //        }
    //    }

    //    private void DispatchWeakEventBucket(
    //        string eventType,
    //        ConcurrentDictionary<Guid, WeakReference<Action<SseEvent>>> bucket,
    //        SseEvent evt)
    //    {
    //        List<Guid>? dead = null;
    //        foreach (var kv in bucket)
    //        {
    //            if (kv.Value.TryGetTarget(out var cb))
    //            {
    //                try { cb(evt); } catch (Exception ex) { _logger?.LogDebug(ex, "Typed listener threw for {EventType}.", eventType); }
    //            }
    //            else
    //            {
    //                (dead ??= new()).Add(kv.Key);
    //            }
    //        }
    //        if (dead is not null)
    //        {
    //            foreach (var d in dead) bucket.TryRemove(d, out _);
    //            if (bucket.IsEmpty)
    //                _typedListeners.TryRemove(eventType, out _);
    //            _logger?.LogTrace("Pruned {Count} dead typed listeners for {EventType}.", dead.Count, eventType);
    //        }
    //    }

    //    private void DispatchWeakInt(
    //        ConcurrentDictionary<Guid, WeakReference<Action<int>>> dict,
    //        int value)
    //    {
    //        if (dict.IsEmpty) return;
    //        List<Guid>? dead = null;
    //        foreach (var kv in dict)
    //        {
    //            if (kv.Value.TryGetTarget(out var cb))
    //            {
    //                try { cb(value); } catch (Exception ex) { _logger?.LogDebug(ex, "Reconnect listener threw."); }
    //            }
    //            else
    //            {
    //                (dead ??= new()).Add(kv.Key);
    //            }
    //        }
    //        if (dead is not null)
    //        {
    //            foreach (var d in dead) dict.TryRemove(d, out _);
    //            _logger?.LogTrace("Pruned {Count} dead reconnect listeners.", dead.Count);
    //        }
    //    }

    //    // Disposal ------------------------------------------------------------------

    //    public async ValueTask DisposeAsync()
    //    {
    //        if (_disposed) return;
    //        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
    //        try
    //        {
    //            if (_module is not null)
    //            {
    //                try { await _module.InvokeVoidAsync("stopSse").ConfigureAwait(false); } catch { }
    //                await _module.DisposeAsync().ConfigureAwait(false);
    //            }

    //            _module = null;
    //            _objRef?.Dispose();
    //            _objRef = null;

    //            _typedListeners.Clear();
    //            _allEventListeners.Clear();
    //            _reconnectAttemptListeners.Clear();
    //            _reconnectSuccessListeners.Clear();

    //            _isStarted = false;
    //            _isConnected = false;
    //            _currentUrl = null;
    //            _disposed = true;
    //            _logger?.LogTrace("SseClient disposed.");
    //        }
    //        finally
    //        {
    //            _lifecycleGate.Release();
    //            _lifecycleGate.Dispose();
    //        }
    //    }

    //    private void ThrowIfDisposed() =>
    //        ObjectDisposedException.ThrowIf(_disposed, nameof(SseClient));
//}