namespace BlazorSseClient.Services
{
    public readonly record struct SseEvent(string EventType, string Data, string? Id);
}
