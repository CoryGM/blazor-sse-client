using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using BlazorSseClient.Services;

namespace BlazorSseClient
{
    public abstract class SseClientBase : IAsyncDisposable
    {
        // SSE message registries
        private readonly ConcurrentDictionary<string, SseEventCallbackBag> _byEventType =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly SseEventCallbackBag _allEvents = new();
        protected readonly ILogger? _logger;

        protected SseClientBase(ILogger? logger = null)
        {
            _logger = logger;
            _logger?.LogTrace("SseClient created.");
        }

        /// <summary>
        /// Subscribe to a specific event type with an async handler (Func)
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="handler"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Guid Subscribe(string eventType, Func<SseEvent, ValueTask> handler, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(eventType);
            var bag = _byEventType.GetOrAdd(eventType, static _ => new SseEventCallbackBag());

            return bag.Add(handler, cancellationToken);
        }

        /// <summary>
        /// Subscribe with a synchronous handler (Action) instead of an async handler (Func)
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="handler"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Guid Subscribe(string eventType, Action<SseEvent> handler, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(eventType);
            var bag = _byEventType.GetOrAdd(eventType, static _ => new SseEventCallbackBag());

            return bag.Add(handler, cancellationToken);
        }

        /// <summary>
        /// Unsubscribe from a specific event type using the subscription ID returned when subscribing.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="id"></param>
        public virtual void Unsubscribe(string eventType, Guid id)
        {
            if (String.IsNullOrWhiteSpace(eventType))
                return;

            if (_byEventType.TryGetValue(eventType, out var bag))
            {
                bag.Remove(id);
            }
        }

        /// <summary>
        /// Remove all subscriptions bound to a specific instance target (if you have it)
        /// </summary>
        /// <param name="owner"></param>
        public void UnsubscribeOwner(object owner)
        {
            if (owner is null) 
                return;

            foreach (var bag in _byEventType.Values)
                bag.RemoveOwner(owner); // optional extension, not strictly needed

            _allEvents.RemoveOwner(owner);
        }

        public virtual ValueTask DisposeAsync()
        {
            _byEventType.Clear();
            _logger?.LogTrace("SseClient disposed.");

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Get the effective URL for starting the SSE listener.
        /// If the url from the is already an absolute URL the assumption
        /// is the user wants to use that instead of concatenating it to the
        /// base address. 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected static string? GetEffectiveUrl(string? url, string? defaultUrl, Dictionary<string, string>? queryParameters = null)
        {
            queryParameters ??= [];

            // If no override, return the default as-is
            if (String.IsNullOrWhiteSpace(url))
                return defaultUrl;

            // Absolute URL wins
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return AppendQuery(url, queryParameters);

            // If there's no default/base, just use the relative url
            if (String.IsNullOrWhiteSpace(defaultUrl))
                return AppendQuery(url, queryParameters);

            // Combine base and relative with exactly one slash
            var basePart = defaultUrl.TrimEnd('/');
            var relPart = url.TrimStart('/');
            var combined = $"{basePart}/{relPart}";

            return AppendQuery(combined, queryParameters);
        }

        private static string AppendQuery(string url, Dictionary<string, string> queryParameters)
        {
            if (queryParameters.Count == 0)
                return url;

            var sep = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            var query = string.Join('&', queryParameters.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            return $"{url}{sep}{query}";
        }

        internal async Task DispatchConnectionStateChangeAsync(SseClientSource source, 
            SseConnectionState state)
        {

        }

        internal async Task DispatchOnMessageAsync(SseClientSource clientSource, SseEvent? sseMessage)
        {
            if (sseMessage is null) return;

            _logger?.LogTrace("SSE Dispatch: Source={Source} Type={Type}", clientSource, sseMessage.Value.EventType);

            var tasks = new List<Task>(2) { _allEvents.InvokeAsync(sseMessage.Value) };

            if (!String.IsNullOrWhiteSpace(sseMessage.Value.EventType) &&
                _byEventType.TryGetValue(sseMessage.Value.EventType, out var bag))
            {
                tasks.Add(bag.InvokeAsync(sseMessage.Value));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
