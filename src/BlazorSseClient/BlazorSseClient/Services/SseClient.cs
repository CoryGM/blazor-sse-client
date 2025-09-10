using System.Collections.Concurrent;

using Microsoft.JSInterop;

namespace BlazorSseClient.Services;

public class SseClient : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private bool _isStarted = false;
    private bool _isConnected = false;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, WeakReference<Action<string, string?>>>> _typedListeners = new();
    private readonly ConcurrentDictionary<Guid, WeakReference<Action<int>>> _reconnectListeners = new();

    public bool IsStarted { get => _isStarted; }
    public bool IsConnected { get => _isConnected; }

    public SseClient(IJSRuntime js)
    {
        _js = js;
    }

    public async Task StartAsync(string endpointBase)
    {
        _module ??= await _js.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorSseClient/js/sse-client.js").ConfigureAwait(false);

        var url = $"{endpointBase}";
        await _module.InvokeVoidAsync("startSse", url, DotNetObjectReference.Create(this)).ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        if (_module is null)
        {
            _isStarted = false;
            return;
        }

        await _module.InvokeVoidAsync("stopSse").ConfigureAwait(false);
    }

    public Guid Subscribe(string eventType, Action<string, string?> callback)
    {
        var id = Guid.NewGuid();
        var weakRef = new WeakReference<Action<string, string?>>(callback);
        var listeners = _typedListeners.GetOrAdd(eventType, _ => new());

        listeners[id] = weakRef;

        return id;
    }

    public void Unsubscribe(string eventType, Guid id)
    {
        if (_typedListeners.TryGetValue(eventType, out var listeners))
        {
            listeners.TryRemove(id, out _);
        }
    }

    public Guid SubscribeReconnect(Action<int> callback)
    {
        var id = Guid.NewGuid();

        _reconnectListeners[id] = new WeakReference<Action<int>>(callback);

        return id;
    }

    public void UnsubscribeReconnect(Guid id)
    {
        _reconnectListeners.TryRemove(id, out _);
    }

    [JSInvokable]
    public void OnSseMessage(string eventType, string message, string? id)
    {
        if (String.IsNullOrWhiteSpace(eventType) ||
            String.IsNullOrWhiteSpace(message))
            return;

        if (_typedListeners.TryGetValue(eventType, out var listeners))
        {
            foreach (var kvp in listeners)
            {
                if (kvp.Value.TryGetTarget(out var callback))
                {
                    callback.Invoke(message, id);
                }
            }
        }
    }

    [JSInvokable]
    public void OnSseStart() => _isStarted = true;

    [JSInvokable]
    public void OnSseStop() => _isStarted = false;

    [JSInvokable]
    public void OnSseConnect() => _isConnected = true;

    [JSInvokable]
    public void OnSseError() => _isConnected = false;

    [JSInvokable]
    public void OnSseReconnect(int attemptCount)
    {
        _isConnected = true;

        foreach (var kvp in _reconnectListeners)
        {
            if (kvp.Value.TryGetTarget(out var callback))
            {
                callback.Invoke(attemptCount);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("stopSse").ConfigureAwait(false);
            await _module.DisposeAsync().ConfigureAwait(false);
        }
        _typedListeners.Clear();
        _reconnectListeners.Clear();
    }
}