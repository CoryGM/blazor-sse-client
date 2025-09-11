namespace BlazorSseClient.Services
{
    /// <summary>
    /// Run state of the SSE client connection. These values
    /// match the values in the JavaScript client.
    /// </summary>
    public enum SseRunState
    {
        Unknown = 0,
        Starting = 1,
        Started = 2,
        Stopping = 3,
        Stopped = 4
    }
}
