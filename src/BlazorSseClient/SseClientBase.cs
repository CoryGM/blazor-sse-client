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
        /// Subscribe to all SSE messages with an async handler (Func)
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Guid SubscribeAll(Func<SseEvent, ValueTask> handler, CancellationToken cancellationToken = default)
            => _allEvents.Add(handler, cancellationToken);

        /// <summary>
        /// Subscribe to all SSE messages with a synchronous handler (Action) instead of an async handler (Func)
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Guid SubscribeAll(Action<SseEvent> handler, CancellationToken cancellationToken = default)
            => _allEvents.Add(handler, cancellationToken);

        /// <summary>
        /// Unsubscribe from all events using the subscription ID returned when subscribing.
        /// </summary>
        /// <param name="id"></param>
        public void UnsubscribeAll(Guid id) => _allEvents.Remove(id);

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
            if (String.IsNullOrWhiteSpace(eventType)) return;
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
            if (owner is null) return;
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

            if (String.IsNullOrWhiteSpace(defaultUrl))
                defaultUrl = String.Empty;

            if (String.IsNullOrWhiteSpace(url))
                return defaultUrl;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            if (String.IsNullOrWhiteSpace(defaultUrl))
                return url;

            // Make sure we don't end up with a double-slash
            var newUrl = $"{(defaultUrl.EndsWith('/') ? defaultUrl + url.TrimEnd('/') : defaultUrl)}" +
                         "//" +
                         $"{(url.StartsWith('/') ? url.TrimStart('/') : url)}";

            if (queryParameters.Count == 0)
                return newUrl;

            var sep = newUrl.Contains('?') ? '&' : '?';
            var queryParams = String.Join('&', queryParameters.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            return $"{newUrl}{sep}{queryParams}";
        }

        internal async Task DispatchOnMessageAsync(SseClientSource clientSource, SseEvent? sseMessage)
        {
            if (sseMessage is null) return;

            Console.WriteLine($"Received SSE Message: {clientSource} - {sseMessage.Value.EventType} - {sseMessage.Value.Data}");

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
