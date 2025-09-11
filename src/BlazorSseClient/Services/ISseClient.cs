namespace BlazorSseClient.Services;

public interface ISseClient : IAsyncDisposable
{
    public readonly record struct SseEvent(string EventType, string Data, string? Id);

    bool IsStarted { get; }
    bool IsConnected { get; }
    string? CurrentUrl { get; }

    Task StartAsync(string url, bool restartOnDifferentUrl = true);
    Task StopAsync();

    Guid Subscribe(string eventType, Action<SseEvent> callback);
    void Unsubscribe(string eventType, Guid id);

    Guid SubscribeAll(Action<SseEvent> callback);
    void UnsubscribeAll(Guid id);

    Guid SubscribeReconnectAttempt(Action<int> callback);
    void UnsubscribeReconnectAttempt(Guid id);

    Guid SubscribeReconnect(Action<int> callback);
    void UnsubscribeReconnect(Guid id);
}