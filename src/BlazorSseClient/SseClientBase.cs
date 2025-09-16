using BlazorSseClient.Services;

namespace BlazorSseClient
{
    public abstract class SseClientBase
    {
        internal async Task DispatchOnMessageAsync(SseClientSource clientSource, SseEvent? sseMessage)
        {
            Console.WriteLine($"Received SSE Message: {clientSource} - {sseMessage?.EventType} - {sseMessage?.Data}");
        }

        internal async Task DispatchConnectionStateChangeAsync(SseClientSource clientSource, SseConnectionState state)
        {
            Console.WriteLine($"Connection State Changed: {clientSource} - {state}");
        }

        internal async Task DispatchRunStateChangeAsync(SseClientSource clientSource, SseRunState state)
        {
            Console.WriteLine($"Run State Changed: {clientSource} - {state}");
        }
    }
}
