namespace BlazorSseClient.Wasm
{
    public sealed class WasmSseClientOptions
    {
        // Optional base address combined with relative URLs.
        public string? BaseAddress { get; set; }

        // Reconnect timing (client JS logic). Exponential-ish with cap + jitter.
        public int ReconnectBaseDelayMs { get; set; } = 1000;
        public int ReconnectMaxDelayMs { get; set; } = 10000;
        public int ReconnectJitterMs { get; set; } = 500;

        // Append query parameters (cannot set arbitrary headers for EventSource).
        public Dictionary<string, string> QueryParameters { get; init; } = [];

        // Whether to send cookies (CORS must allow credentials).
        public bool UseCredentials { get; set; }

        // Whether to start the connection automatically on construction if BaseAddress is set.
        public bool AutoStart { get; set; } = false;

        public void Validate()
        {
            if (ReconnectBaseDelayMs <= 0) throw new ArgumentOutOfRangeException(nameof(ReconnectBaseDelayMs));
            if (ReconnectMaxDelayMs < ReconnectBaseDelayMs) throw new ArgumentOutOfRangeException(nameof(ReconnectMaxDelayMs));
            if (ReconnectJitterMs < 0) throw new ArgumentOutOfRangeException(nameof(ReconnectJitterMs));
        }
    }
}
