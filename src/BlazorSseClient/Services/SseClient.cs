using System.Collections.Concurrent;

using Microsoft.JSInterop;

namespace BlazorSseClient.Services;

public sealed class SseClient : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<SseClient>? _objRef;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    private volatile bool _isStarted;
    private volatile bool _isConnected;
    private string? _currentUrl;
    private bool _disposed;

    // Weak-reference listeners (typed and "all events")
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WeakReference<Action<SseEvent>>>> _typedListeners = new();
    private readonly ConcurrentDictionary<Guid, WeakReference<Action<SseEvent>>> _allEventListeners = new();

    // Reconnect weak listeners
    private readonly ConcurrentDictionary<Guid, WeakReference<Action<int>>> _reconnectAttemptListeners = new();
    private readonly ConcurrentDictionary<Guid, WeakReference<Action<int>>> _reconnectSuccessListeners = new();

    public bool IsStarted => _isStarted;
    public bool IsConnected => _isConnected;
    public string? CurrentUrl => _currentUrl;

    public SseClient(IJSRuntime js)
    {
        _js = js;
        _objRef = DotNetObjectReference.Create(this);
    }

    // Lifecycle ----------------------------------------------------------------

    public async Task StartAsync(string url, bool restartOnDifferentUrl = true)
    {
        ThrowIfDisposed();

        await _lifecycleGate.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_isStarted && string.Equals(_currentUrl, url, StringComparison.Ordinal))
                return;

            if (_isStarted && !string.Equals(_currentUrl, url, StringComparison.Ordinal))
            {
                if (!restartOnDifferentUrl)
                    return;

                await InternalStopAsync().ConfigureAwait(false);
            }

            _module ??= await _js
                .InvokeAsync<IJSObjectReference>("import", "./_content/BlazorSseClient/js/sse-client.js")
                .ConfigureAwait(false);

            _currentUrl = url;

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
            try { 
                await _module.InvokeVoidAsync("stopSse").ConfigureAwait(false); 
            }
            catch 
            { 
                /* ignore */ 
            }
        }

        _isStarted = false;
        _isConnected = false;
        _currentUrl = null;
    }

    // Subscription API (weak) --------------------------------------------------

    public Guid Subscribe(string eventType, Action<SseEvent> callback)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type must be non-empty.", nameof(eventType));

        ArgumentNullException.ThrowIfNull(callback);

        var id = Guid.NewGuid();
        var map = _typedListeners.GetOrAdd(eventType, _ => new());

        map[id] = new WeakReference<Action<SseEvent>>(callback);

        return id;
    }

    public void Unsubscribe(string eventType, Guid id)
    {
        if (_typedListeners.TryGetValue(eventType, out var map))
        {
            map.TryRemove(id, out _);
            if (map.IsEmpty)
                _typedListeners.TryRemove(eventType, out _);
        }
    }

    public Guid SubscribeAll(Action<SseEvent> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var id = Guid.NewGuid();

        _allEventListeners[id] = new WeakReference<Action<SseEvent>>(callback);

        return id;
    }

    public void UnsubscribeAll(Guid id) => _allEventListeners.TryRemove(id, out _);

    public Guid SubscribeReconnectAttempt(Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var id = Guid.NewGuid();

        _reconnectAttemptListeners[id] = new WeakReference<Action<int>>(callback);

        return id;
    }

    public void UnsubscribeReconnectAttempt(Guid id) =>
        _reconnectAttemptListeners.TryRemove(id, out _);

    public Guid SubscribeReconnect(Action<int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var id = Guid.NewGuid();

        _reconnectSuccessListeners[id] = new WeakReference<Action<int>>(callback);

        return id;
    }

    public void UnsubscribeReconnect(Guid id) =>
        _reconnectSuccessListeners.TryRemove(id, out _);

    // JS -> .NET callbacks ------------------------------------------------------

    [JSInvokable]
    public void OnSseMessage(string eventType, string data, string? id)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            return;

        var evt = new SseEvent(eventType, data, id);

        // All-event listeners
        if (!_allEventListeners.IsEmpty)
        {
            List<Guid>? dead = null;

            foreach (var kvp in _allEventListeners)
            {
                if (kvp.Value.TryGetTarget(out var callback))
                {
                    try { 
                        callback(evt); 
                    } 
                    catch 
                    { 
                    }
                }
                else
                {
                    (dead ??= new()).Add(kvp.Key);
                }
            }

            if (dead is not null)
                foreach (var d in dead) 
                    _allEventListeners.TryRemove(d, out _);
        }

        // Typed listeners
        if (_typedListeners.TryGetValue(eventType, out var map) && !map.IsEmpty)
        {
            List<Guid>? dead = null;
            foreach (var kvp in map)
            {
                if (kvp.Value.TryGetTarget(out var cb))
                {
                    try { cb(evt); } catch { }
                }
                else
                {
                    (dead ??= new()).Add(kvp.Key);
                }
            }
            if (dead is not null)
            {
                foreach (var d in dead)
                    map.TryRemove(d, out _);
                if (map.IsEmpty)
                    _typedListeners.TryRemove(eventType, out _);
            }
        }
    }

    [JSInvokable] public void OnSseStart() => _isStarted = true;
    [JSInvokable] public void OnSseStop() { _isStarted = false; _isConnected = false; }
    [JSInvokable] public void OnSseConnect() => _isConnected = true;
    [JSInvokable] public void OnSseError() => _isConnected = false;

    [JSInvokable]
    public void OnSseReconnectAttempt(int attemptCount)
    {
        _isConnected = false;
        DispatchWeakInt(_reconnectAttemptListeners, attemptCount);
    }

    [JSInvokable]
    public void OnSseReconnect(int attemptCount)
    {
        _isConnected = true;
        DispatchWeakInt(_reconnectSuccessListeners, attemptCount);
    }

    private static void DispatchWeakInt(
        ConcurrentDictionary<Guid, WeakReference<Action<int>>> dict, int value)
    {
        if (dict.IsEmpty) return;
        
        List<Guid>? dead = null;

        foreach (var kvp in dict)
        {
            if (kvp.Value.TryGetTarget(out var cb))
            {
                try { cb(value); } catch { }
            }
            else
            {
                (dead ??= new()).Add(kvp.Key);
            }
        }

        if (dead is not null)
            foreach (var d in dead)
                dict.TryRemove(d, out _);
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
                try { 
                    await _module.InvokeVoidAsync("stopSse").ConfigureAwait(false); 
                } 
                catch 
                { 
                }

                await _module.DisposeAsync().ConfigureAwait(false);
            }

            _module = null;
            _isStarted = false;
            _isConnected = false;
            _currentUrl = null;

            _typedListeners.Clear();
            _allEventListeners.Clear();
            _reconnectAttemptListeners.Clear();
            _reconnectSuccessListeners.Clear();

            _objRef?.Dispose();
            _objRef = null;
            _disposed = true;
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