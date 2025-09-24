using BlazorSseClient.Services;
using System.Collections.Concurrent;
using System.Reflection;

namespace BlazorSseClient
{
    // SSE-only callback registry with weak instance targets and async fan-out
    internal sealed class SseEventCallbackBag
    {
        private sealed class Entry
        {
            public WeakReference<object>? TargetRef; // null => static handler
            public required Func<object?, SseEvent, ValueTask> Invoker;
        }

        private readonly ConcurrentDictionary<Guid, Entry> _map = new();

        public Guid Add(Func<SseEvent, ValueTask> handler, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(handler);

            var (targetRef, invoker) = BuildInvoker(handler);

            return AddCore(targetRef, invoker, token);
        }

        public Guid Add(Action<SseEvent> handler, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(handler);

            var (targetRef, invoker) = BuildInvoker(handler);

            return AddCore(targetRef, invoker, token);
        }

        public void Remove(Guid id) => _map.TryRemove(id, out _);

        // Optional bulk removal if you track owner instances explicitly
        public int RemoveOwner(object owner)
        {
            if (owner is null) return 0;
            var removed = 0;
            foreach (var (id, entry) in _map)
            {
                if (entry.TargetRef is not null &&
                    entry.TargetRef.TryGetTarget(out var target) &&
                    ReferenceEquals(target, owner))
                {
                    if (_map.TryRemove(id, out _)) removed++;
                }
            }
            return removed;
        }

        public Task InvokeAsync(SseEvent evt)
        {
            if (_map.IsEmpty)
                return Task.CompletedTask;

            List<Task>? tasks = null;

            foreach (var (id, entry) in _map)
            {
                try
                {
                    if (entry.TargetRef is null)
                    {
                        tasks ??= new List<Task>();
                        tasks.Add(InvokeOneAsync(entry.Invoker, null, evt));
                    }
                    else if (entry.TargetRef.TryGetTarget(out var owner))
                    {
                        tasks ??= new List<Task>();
                        tasks.Add(InvokeOneAsync(entry.Invoker, owner, evt));
                    }
                    else
                    {
                        // Instance target was collected; prune the subscription
                        _map.TryRemove(id, out _);
                    }
                }
                catch
                {
                    // Keep fan-out robust; consider logging
                }
            }

            if (tasks is null || tasks.Count == 0)
                return Task.CompletedTask;

            return Task.WhenAll(tasks);
        }

        private static async Task InvokeOneAsync(Func<object?, SseEvent, ValueTask> cb, object? owner, SseEvent value)
        {
            try
            {
                await cb(owner, value).ConfigureAwait(false);
            }
            catch
            {
                // Isolate subscriber exceptions; consider logging
            }
        }

        private Guid AddCore(WeakReference<object>? targetRef, Func<object?, SseEvent, ValueTask> invoker, CancellationToken token)
        {
            var id = Guid.NewGuid();

            _map[id] = new Entry { TargetRef = targetRef, Invoker = invoker };

            if (token.CanBeCanceled)
            {
                token.Register(static state =>
                {
                    var (map, guid) = ((ConcurrentDictionary<Guid, Entry>, Guid))state!;
                    map.TryRemove(guid, out _);
                }, (_map, id));
            }

            return id;
        }

        private static (WeakReference<object>? targetRef, Func<object?, SseEvent, ValueTask> invoker)
            BuildInvoker(Func<SseEvent, ValueTask> handler)
        {
            if (handler.Target is null)
            {
                // Static async handler
                return (null, (_, evt) => handler(evt));
            }

            var target = handler.Target!;
            var mi = handler.GetMethodInfo();

            if (mi.IsStatic)
            {
                // Shouldn't happen when Target != null, but handle defensively
                return (null, (_, evt) => handler(evt));
            }

            // Build an open instance delegate: Func<TTarget, SseEvent, ValueTask>
            var targetType = target.GetType();
            var delType = typeof(Func<,,>).MakeGenericType(targetType, typeof(SseEvent), typeof(ValueTask));
            var openDel = Delegate.CreateDelegate(delType, null, mi);

            // Wrap into a uniform invoker
            ValueTask Inv(object? obj, SseEvent evt)
                => (ValueTask)openDel.DynamicInvoke(obj!, evt)!;

            return (new WeakReference<object>(target), Inv);
        }

        private static (WeakReference<object>? targetRef, Func<object?, SseEvent, ValueTask> invoker)
            BuildInvoker(Action<SseEvent> handler)
        {
            if (handler.Target is null)
            {
                // Static sync handler
                return (null, (_, evt) =>
                {
                    handler(evt);
                    return ValueTask.CompletedTask;
                });
            }

            var target = handler.Target!;
            var mi = handler.GetMethodInfo();

            if (mi.IsStatic)
            {
                return (null, (_, evt) =>
                {
                    handler(evt);
                    return ValueTask.CompletedTask;
                });
            }

            // Build an open instance delegate: Action<TTarget, SseEvent>
            var targetType = target.GetType();
            var delType = typeof(Action<,>).MakeGenericType(targetType, typeof(SseEvent));
            var openDel = Delegate.CreateDelegate(delType, null, mi);

            // Wrap into a uniform invoker
            ValueTask Inv(object? obj, SseEvent evt)
            {
                openDel.DynamicInvoke(obj!, evt);
                return ValueTask.CompletedTask;
            }

            return (new WeakReference<object>(target), Inv);
        }
    }
}
