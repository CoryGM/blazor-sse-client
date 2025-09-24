namespace BlazorSseClient.Services
{
    /// <summary>
    /// Run state of the SSE client connection. These values
    /// match the values in the JavaScript client.
    /// </summary>
    public enum SseConnectionState
    {
        Unknown = 0,
        Opening = 1,
        Open = 2,
        Reopening = 3,
        Reopened = 4,       //  Not actually a state, just a notification. Reopened goes to Opened.
        Closed = 5
    }
}
