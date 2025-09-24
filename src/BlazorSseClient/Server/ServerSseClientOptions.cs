namespace BlazorSseClient.Server
{
    public sealed class ServerSseClientOptions
    {
        public string? BaseAddress { get; set; }
    
        // I need this set to the max value for an HttpClient timeout
        // because the HttpClient is used for a long-running request.
        // Ignored for WasmSseClient.
        public TimeSpan Timeout { get; set; } = System.Threading.Timeout.InfiniteTimeSpan;

        // Optional static request headers (e.g. Authorization, custom keys).
        public Dictionary<string, string> DefaultRequestHeaders { get; init; } = [];

        // Reconnect timing (server logic). Exponential-ish with cap + jitter.
        public int ReconnectBaseDelayMs { get; set; } = 1000;
        public int ReconnectMaxDelayMs { get; set; } = 10000;
        public int ReconnectJitterMs { get; set; } = 500;

        // Validate options (call manually or via extension).
        public void Validate()
        {
            if (Timeout != System.Threading.Timeout.InfiniteTimeSpan && Timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(Timeout), "Timeout must be positive or InfiniteTimeSpan.");

            if (ReconnectBaseDelayMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(ReconnectBaseDelayMs));

            if (ReconnectMaxDelayMs < ReconnectBaseDelayMs)
                throw new ArgumentOutOfRangeException(nameof(ReconnectMaxDelayMs));

            if (ReconnectJitterMs < 0)
                throw new ArgumentOutOfRangeException(nameof(ReconnectJitterMs));
        }
    }
}
