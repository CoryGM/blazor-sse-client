using BlazorSseClient.Services;

namespace BlazorSseClient;

public interface ISseClient : IAsyncDisposable
{
    SseRunState RunState { get; }
    SseConnectionState ConnectionState { get; }

    Task StartAsync(string? url = null, bool restartOnDifferentUrl = true);
    Task StopAsync();

    Guid Subscribe(string eventType, Func<SseEvent, ValueTask> handler, CancellationToken cancellationToken = default);
    Guid Subscribe(string eventType, Action<SseEvent> handler, CancellationToken cancellationToken = default);
    void Unsubscribe(string eventType, Guid id);
    void UnsubscribeOwner(object owner);

    Guid SubscribeConnectionStateChange(Func<SseEvent, ValueTask> handler, CancellationToken cancellationToken = default);
}
