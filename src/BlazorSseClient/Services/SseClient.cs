using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorSseClient.Services;

/// <summary>
/// Browser (EventSource via JS) SSE client using weak-reference subscriptions.
/// </summary>
public sealed class SseClient : ISseClient
{
    public readonly record struct SseEvent(string EventType, string Data, string? Id);

    private readonly IJSRuntime _js;
    private readonly ILogger<SseClient>? _logger;
    private IJSObjectReference? _module;
    private DotNetObjectReference<SseClient>? _objRef;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    private volatile bool _isStarted;
    private volatile bool _isConnected;
    private string? _currentUrl;
    private bool _disposed;

    // Weak-reference listener registries
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WeakReference<Action<SseEvent>>>> _typedListeners = new();
    private readonly ConcurrentDictionary<Guid, WeakReference<Action<SseEvent>>> _allEventListeners = new();
    private readonly ConcurrentDictionary<Guid, WeakReference<Action<int>>> _reconnectAttemptListeners = new();
    private readonly ConcurrentDictionary<Guid, WeakReference<Action<int>>> _reconnectSuccessListeners = new();

    public bool IsStarted => _isStarted;
    public bool IsConnected => _isConnected;
    public string? CurrentUrl => _currentUrl;

    public SseClient(IJSRuntime js, ILogger<SseClient>? logger = null)
    {
        _js = js;
        _logger = logger;
        _objRef = DotNetObjectReference.Create(this);
        _logger?.LogTrace("SseClient constructed.");
    }

    // Lifecycle ----------------------------------------------------------------

    public async Task StartAsync(string url, bool restartOnDifferentUrl = true)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isStarted && string.Equals(_currentUrl, url, StringComparison.Ordinal))
            {
                _logger?.LogDebug("StartAsync ignored (already started): {Url}", url);
                return;
            }

            if (_isStarted && !string.Equals(_currentUrl, url, StringComparison.Ordinal))
            {
                if (!restartOnDifferentUrl)
                {
                    _logger?.LogDebug("StartAsync new URL {New} while running {Old} (restart disabled).", url, _currentUrl);
                    return;
                }
                _logger?.LogInformation("Restarting SSE from {Old} to {New}", _currentUrl, url);
                await InternalStopAsync().ConfigureAwait(false);
            }

            if (_module is null)
            {
                _logger?.LogTrace("Importing JS module.");
                _module = await _js.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/BlazorSseClient/js/sse-client.js").ConfigureAwait(false);
            }

            _currentUrl = url;
            _logger?.LogInformation("Starting SSE connection to {Url}", url);
            await _module.InvokeVoidAsync("startSse", url, _objRef).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync()
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await InternalStopAsync().ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task InternalStopAsync()
    {
        if (_module is not null && _isStarted)
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
        _isStarted = false;
        _isConnected = false;
        _currentUrl = null;
        _logger?.LogTrace("InternalStopAsync: state cleared.");
    }

    // Subscription API (weak) --------------------------------------------------

    public Guid Subscribe(string eventType, Action<SseEvent> callback)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type must be non-empty.", nameof(eventType));
        ArgumentNullException.ThrowIfNull(callback);

        var id = Guid.NewGuid();
        var bucket = _typedListeners.GetOrAdd(eventType, _ => new());
        bucket[id] = new WeakReference<Action<SseEvent>>(callback);
        _logger?.LogTrace("Subscribed typed listener {Id} -> {Event}", id, eventType);
        return id;
    }

    public void Unsubscribe(string eventType, Guid id)
    {
        if (_typedListeners.TryGetValue(eventType, out var bucket))
        {
            bucket.TryRemove(id, out _);
            if (bucket.IsEmpty)
                _typedListeners.TryRemove(eventType, out _);
            _logger?.LogTrace("Unsubscribed typed listener {Id} from {Event}", id, eventType);
        }
    }

    public Guid SubscribeAll(Action<SseEvent> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var id = Guid.NewGuid();
        _allEventListeners[id] = new WeakReference<Action<SseEvent>>(callback);
        _logger?.LogTrace("Subscribed all-events listener {Id}", id);
        return id;
    }

    public void UnsubscribeAll(Guid id)
    {
        if (_allEventListeners.TryRemove(id, out _))
            _logger?.LogTrace("Unsubscribed all-events listener {Id}", id);
    }

    public Guid SubscribeReconnectAttempt(Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var id = Guid.NewGuid();
        _reconnectAttemptListeners[id] = new WeakReference<Action<int>>(callback);
        _logger?.LogTrace("Subscribed reconnect-attempt listener {Id}", id);
        return id;
    }

    public void UnsubscribeReconnectAttempt(Guid id)
    {
        _reconnectAttemptListeners.TryRemove(id, out _);
    }

    public Guid SubscribeReconnect(Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var id = Guid.NewGuid();
        _reconnectSuccessListeners[id] = new WeakReference<Action<int>>(callback);
        _logger?.LogTrace("Subscribed reconnect-success listener {Id}", id);
        return id;
    }

    public void UnsubscribeReconnect(Guid id)
    {
        _reconnectSuccessListeners.TryRemove(id, out _);
    }

    // JS -> .NET callbacks ------------------------------------------------------

    [JSInvokable] public void OnSseStart() { _isStarted = true; _logger?.LogDebug("SSE start signal."); }
    [JSInvokable] public void OnSseStop() { _isStarted = false; _isConnected = false; _logger?.LogDebug("SSE stop signal."); }
    [JSInvokable] public void OnSseConnect() { _isConnected = true; _logger?.LogInformation("SSE connected."); }
    [JSInvokable] public void OnSseError() { _isConnected = false; _logger?.LogWarning("SSE error (disconnected)."); }

    [JSInvokable]
    public void OnSseReconnectAttempt(int attempt)
    {
        _isConnected = false;
        _logger?.LogDebug("Reconnect attempt #{Attempt}", attempt);
        DispatchWeakInt(_reconnectAttemptListeners, attempt);
    }

    [JSInvokable]
    public void OnSseReconnect(int attempts)
    {
        _isConnected = true;
        _logger?.LogInformation("Reconnected after {Attempts} attempt(s).", attempts);
        DispatchWeakInt(_reconnectSuccessListeners, attempts);
    }

    [JSInvokable]
    public void OnSseMessage(string eventType, string data, string? id)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            return;

        var evt = new SseEvent(eventType, data, id);

        // All-event listeners
        if (!_allEventListeners.IsEmpty)
            DispatchWeakEventCollection(_allEventListeners, evt, isAll: true);

        // Typed listeners
        if (_typedListeners.TryGetValue(eventType, out var bucket) && !bucket.IsEmpty)
            DispatchWeakEventBucket(eventType, bucket, evt);

        if (_logger?.IsEnabled(LogLevel.Trace) == true)
        {
            var preview = data.Length > 64 ? data[..64] + "…" : data;
            _logger.LogTrace("Dispatched {Event} (Id={Id}, Data='{Preview}')", eventType, id ?? "null", preview);
        }
    }

    // Dispatch helpers ---------------------------------------------------------

    private void DispatchWeakEventCollection(
        ConcurrentDictionary<Guid, WeakReference<Action<SseEvent>>> dict,
        SseEvent evt,
        bool isAll)
    {
        List<Guid>? dead = null;
        foreach (var kv in dict)
        {
            if (kv.Value.TryGetTarget(out var cb))
            {
                try { cb(evt); } catch (Exception ex) { _logger?.LogDebug(ex, "All-event listener threw."); }
            }
            else
            {
                (dead ??= new()).Add(kv.Key);
            }
        }
        if (dead is not null)
        {
            foreach (var d in dead) dict.TryRemove(d, out _);
            _logger?.LogTrace("Pruned {Count} dead {Scope} listeners.", dead.Count, isAll ? "all-event" : "typed");
        }
    }

    private void DispatchWeakEventBucket(
        string eventType,
        ConcurrentDictionary<Guid, WeakReference<Action<SseEvent>>> bucket,
        SseEvent evt)
    {
        List<Guid>? dead = null;
        foreach (var kv in bucket)
        {
            if (kv.Value.TryGetTarget(out var cb))
            {
                try { cb(evt); } catch (Exception ex) { _logger?.LogDebug(ex, "Typed listener threw for {EventType}.", eventType); }
            }
            else
            {
                (dead ??= new()).Add(kv.Key);
            }
        }
        if (dead is not null)
        {
            foreach (var d in dead) bucket.TryRemove(d, out _);
            if (bucket.IsEmpty)
                _typedListeners.TryRemove(eventType, out _);
            _logger?.LogTrace("Pruned {Count} dead typed listeners for {EventType}.", dead.Count, eventType);
        }
    }

    private void DispatchWeakInt(
        ConcurrentDictionary<Guid, WeakReference<Action<int>>> dict,
        int value)
    {
        if (dict.IsEmpty) return;
        List<Guid>? dead = null;
        foreach (var kv in dict)
        {
            if (kv.Value.TryGetTarget(out var cb))
            {
                try { cb(value); } catch (Exception ex) { _logger?.LogDebug(ex, "Reconnect listener threw."); }
            }
            else
            {
                (dead ??= new()).Add(kv.Key);
            }
        }
        if (dead is not null)
        {
            foreach (var d in dead) dict.TryRemove(d, out _);
            _logger?.LogTrace("Pruned {Count} dead reconnect listeners.", dead.Count);
        }
    }

    // Disposal ------------------------------------------------------------------

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_module is not null)
            {
                try { await _module.InvokeVoidAsync("stopSse").ConfigureAwait(false); } catch { }
                await _module.DisposeAsync().ConfigureAwait(false);
            }

            _module = null;
            _objRef?.Dispose();
            _objRef = null;

            _typedListeners.Clear();
            _allEventListeners.Clear();
            _reconnectAttemptListeners.Clear();
            _reconnectSuccessListeners.Clear();

            _isStarted = false;
            _isConnected = false;
            _currentUrl = null;
            _disposed = true;
            _logger?.LogTrace("SseClient disposed.");
        }
        finally
        {
            _lifecycleGate.Release();
            _lifecycleGate.Dispose();
        }
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, nameof(SseClient));
}