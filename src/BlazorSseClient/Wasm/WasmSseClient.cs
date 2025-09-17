using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using BlazorSseClient.Services;

namespace BlazorSseClient.Wasm;

public sealed class WasmSseClient : SseClientBase, ISseClient, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<CallbackSink>? _objRef;
    private readonly CallbackSink _sink;
    private readonly ILogger<WasmSseClient>? _logger;
    private SseRunState _runState = SseRunState.Stopped;
    private SseConnectionState _connectionState = SseConnectionState.Closed;
    private bool _disposed = false;
    private string? _currentUrl = null;
    private string? _baseAddress = null;
    private readonly WasmSseClientOptions _options;

    public SseRunState RunState { get => _runState; }
    public SseConnectionState ConnectionState { get => _connectionState; }

    public WasmSseClient(IJSRuntime js, IOptions<WasmSseClientOptions>? options, ILogger<WasmSseClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(js, nameof(js));

        _options = options?.Value ?? new WasmSseClientOptions();

        _js = js;
        _baseAddress = options?.Value.BaseAddress;
        _logger = logger;
        _sink = new CallbackSink(this, logger);
        _objRef = DotNetObjectReference.Create(_sink);

        _logger?.LogTrace("WasmSseClient constructed. Default Url: {BaseAddress}; AutoStart: {AutoStart}", 
            _baseAddress ?? "None", options?.Value.AutoStart);

        if (options?.Value.AutoStart == true && !String.IsNullOrWhiteSpace(_baseAddress))
        {
            _ = StartAsync(_baseAddress!, false);
        }
    }

    /// <summary>
    /// Starts the SSE connection to the specified URL.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="restartOnDifferentUrl"></param>
    /// <returns></returns>
    public async Task StartAsync(string? url, bool restartOnDifferentUrl = true)
    {
        var effectiveUrl = GetEffectiveUrl(url);

        if (String.IsNullOrWhiteSpace(effectiveUrl))
            throw new ArgumentException("URL is required.", nameof(url));

        if (_runState == SseRunState.Started)
        {
            if (!restartOnDifferentUrl || string.Equals(effectiveUrl, _currentUrl, StringComparison.OrdinalIgnoreCase))
            {
                _logger?.LogDebug("SSE already started; ignoring StartAsync.");
                return;
            }

            await InternalStopAsync().ConfigureAwait(false);
        }

        _currentUrl = effectiveUrl;

        if (_module is null)
        {
            _logger?.LogTrace("Importing JS module.");

            _module = await _js.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorSseClient/js/sse-client.js").ConfigureAwait(false);
        }

        _logger?.LogInformation("Starting SSE connection to {CurrentUrl}", _currentUrl);

        var payload = new
        {
            reconnectBaseDelayMs = _options.ReconnectBaseDelayMs,
            reconnectMaxDelayMs = _options.ReconnectMaxDelayMs,
            reconnectJitterMs = _options.ReconnectJitterMs,
            useCredentials = _options.UseCredentials
        };

        await _module.InvokeVoidAsync("startSse", _currentUrl, _objRef, payload).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops the client from listening for events from the server.
    /// </summary>
    /// <returns></returns>
    public async Task StopAsync()
    {
        await InternalStopAsync().ConfigureAwait(false);
    }

    private async Task InternalStopAsync()
    {
        if (_module is not null)
        {
            try
            {
                _logger?.LogInformation("Stopping SSE (URL {Url})", _currentUrl);
                await _module.InvokeVoidAsync("stopSse").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Ignoring stopSse error during shutdown.");
            }
        }

        _currentUrl = null;
        _logger?.LogTrace("InternalStopAsync: state cleared.");
    }

    /// <summary>
    /// Get the effective URL for starting the SSE listener.
    /// If the url from the is already an absolute URL the assumption
    /// is the user wants to use that instead of concatenating it to the
    /// base address. 
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private string? GetEffectiveUrl(string? url)
    {
        if (String.IsNullOrWhiteSpace(url))
            return _baseAddress;

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;

        if (String.IsNullOrWhiteSpace(_baseAddress))
            return url;

        // Make sure we don't end up with a double-slash
        var newUrl = $"{(_baseAddress.EndsWith('/') ? _baseAddress + url.TrimEnd('/') : _baseAddress)}" +
                     "//" +
                     $"{(url.StartsWith('/') ? url.TrimStart('/') : url)}";

        if (_options.QueryParameters.Count == 0)
            return newUrl;

        var sep = newUrl.Contains('?') ? '&' : '?';
        var queryParams = String.Join('&', _options.QueryParameters.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{newUrl}{sep}{queryParams}";
    }

    private void DispatchRunStateChange(SseRunState state)
    {
        _runState = state;

        _logger?.LogDebug("WASM SSE run state: {State}", RunState);

        _ = Task.Run(async () =>
        {
            try
            {
                await DispatchRunStateChangeAsync(SseClientSource.Wasm, state).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error dispatching Run State Change from WASM.");
            }
        });
    }

    private void DispatchConnectionStateChange(SseConnectionState state)
    {
        _connectionState = state == SseConnectionState.Reopened ? SseConnectionState.Open :
                                                                  state;

        _logger?.LogDebug("WASM SSE connection state: {State}", ConnectionState);

        _ = Task.Run(async () =>
        {
            try
            {
                await DispatchConnectionStateChangeAsync(SseClientSource.Wasm, state).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error dispatching Connection State Change from WASM.");
            }
        });
    }

    private void DispatchOnMessage(SseEvent sseEvent)
    {
        _logger?.LogInformation("WASM SSE message: Id={Id} Type={Type}", sseEvent.Id, sseEvent.EventType);

        _ = Task.Run(async () =>
        {
            try
            {
                await DispatchOnMessageAsync(SseClientSource.Wasm, sseEvent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error dispatching SSE message from WASM.");
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await InternalStopAsync().ConfigureAwait(false);

        if (_module is not null)
            await _module.DisposeAsync().ConfigureAwait(false);

        _module = null;
        _objRef?.Dispose();
        _objRef = null;

        _disposed = true;

        _logger?.LogTrace("SseClient disposed.");
    }

    /// <summary>
    /// Proxy class for JS -> .NET callbacks. This class is instantiated
    /// by the SseClient and passed to JS as a DotNetObjectReference. This is
    /// done to prevent the JSInvokable methods from appearing on the SseClient's
    /// public signature. 
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="logger"></param>
    private sealed class CallbackSink(WasmSseClient parent, ILogger<WasmSseClient>? logger)
    {
        private readonly WasmSseClient _parent = parent;
        private readonly ILogger<WasmSseClient>? _logger = logger;

        /// <summary>
        /// Handler for run state changes from JS.
        /// </summary>
        /// <param name="runState"></param>
        [JSInvokable]
        public void OnSseRunStateChange(int? runState)
        {
            if (runState is null)
                return;

            if (Enum.TryParse<SseRunState>($"{runState}", true, out var state))
            {
                _logger?.LogInformation("SSE run state changed to {State}", state);

                //  Dispatch event to listeners here
                _parent.DispatchRunStateChange(state);
            }
            else
            {
                _logger?.LogWarning("Received unknown SSE run state: {State}", runState);
            }
        }

        /// <summary>
        /// Handler for connection state changes from JS.
        /// </summary>
        /// <param name="connectionState"></param>
        [JSInvokable]
        public void OnSseConnectionStateChange(int? connectionState)
        {
            if (connectionState is null)
                return;

            if (Enum.TryParse<SseConnectionState>($"{connectionState}", true, out var state))
            {
                _logger?.LogInformation("SSE connection state changed to {State}", state);

                //  Dispatch event to listeners here
                _parent.DispatchConnectionStateChange(state);
            }
            else
            {
                _logger?.LogWarning("Received unknown SSE connection state: {State}", connectionState);
            }
        }

        /// <summary>
        /// Handler for incoming SSE messages from JS.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="data"></param>
        /// <param name="id"></param>
        [JSInvokable]
        public void OnSseMessage(string eventType, string? data, string? id)
        {
            if (string.IsNullOrWhiteSpace(eventType))
                return;

            var sseEvent = new SseEvent(eventType, data ?? string.Empty, id);

            var preview = data?.Length > 64 ? data[..64] + "…" : data;
            _logger?.LogTrace("Dispatched {Event} (Id={Id}, Data='{Preview}')", eventType, id ?? "null", preview);

            _parent.DispatchOnMessage(sseEvent);
        }
    }
}
