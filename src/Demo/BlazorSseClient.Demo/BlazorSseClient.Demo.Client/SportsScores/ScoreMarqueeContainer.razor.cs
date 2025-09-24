using System.Text.Json;
using BlazorSseClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorSseClient.Demo.Client.SportsScores
{
    /// <summary>
    /// A conveyor belt style marquee component that displays sports scores in a continuous scrolling fashion.
    /// 
    /// Features:
    /// - Items move at a constant speed with minimum spacing between them
    /// - No item overtakes another (first-in-first-out queue system)
    /// - Items are removed after scrolling across (no wrapping)
    /// - Dynamic number of items based on available width
    /// - Works on both server and client-side rendering
    /// - Responsive design with accessibility support
    /// 
    /// Configuration:
    /// - ItemSpacingMs: Minimum time between items starting their journey
    /// - ItemDurationMs: Total time for an item to scroll across the screen
    /// </summary>
    public partial class ScoreMarqueeContainer : ComponentBase, IAsyncDisposable
    {
        [Inject]
        private ISseClient SseClient { get; set; } = null!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        
        // Queue for incoming scores - these haven't started their journey yet
        private readonly Queue<ScoreModel> _incomingScores = new();
        
        // All items on the conveyor belt - using a single list that maintains order
        private readonly List<ConveyorBeltItem> _conveyorBeltItems = new();
        
        // Timer for managing the conveyor belt
        private Timer? _conveyorTimer;
        
        private const string _messageType = "Score";
        private Guid? _scoreSubscriptionId;
        
        // Configuration - adjust these values to change belt behavior
        private const int _itemSpacingMs = 2200; // 2.2 seconds between items starting
        private const int _itemDurationMs = 12000; // 12 seconds for full scroll across
        private const int _cleanupBufferMs = 1000; // Extra time before removing items from memory (increased for safety)
        
        private int _nextItemId = 0;
        private IJSObjectReference? _jsModule;
        private readonly object _lockObject = new();
        private long _lastItemStartTime = 0;
        private string _renderLocation = String.Empty;

        protected override void OnInitialized()
        {
            if (OperatingSystem.IsBrowser())
                _renderLocation = "Browser";
            else
                _renderLocation = "Server";
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                try
                {
                    // Try to load JS module if available (for enhanced timing)
                    _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", 
                        "./_content/BlazorSseClient.Demo.Client/SportsScores/ScoreBanner.razor.js");
                }
                catch
                {
                    // Fallback to pure CSS if JS module loading fails
                    // This ensures the component works even if JS is disabled
                }

                _scoreSubscriptionId = SseClient.Subscribe(_messageType, AddScore);
                StartConveyorBelt();
            }
        }

        private void AddScore(SseEvent sseEvent)
        {
            try
            {
                var score = JsonSerializer.Deserialize<ScoreModel?>(sseEvent.Data, _jsonOptions);

                if (score is null)
                    return;

                lock (_lockObject)
                {
                    // Check if we already have this score anywhere (avoid duplicates)
                    if (_incomingScores.Any(s => s.Id == score.Value.Id) || 
                        _conveyorBeltItems.Any(item => item.Score.Id == score.Value.Id))
                        return;

                    _incomingScores.Enqueue(score.Value);
                    
                    // Debug: Log queue status
                    Console.WriteLine($"[ScoreBanner] Score added to queue. Queue size: {_incomingScores.Count}, Active items: {_conveyorBeltItems.Count(i => !i.IsExpired)}");
                }
            }
            catch (JsonException)
            {
                // Log or handle the error as needed
                return;
            }
        }

        private void StartConveyorBelt()
        {
            // Use a timer to regularly check for updates
            _conveyorTimer = new Timer(ProcessConveyorBelt, null, 0, 100); // Check every 100ms
        }

        private void ProcessConveyorBelt(object? state)
        {
            var needsUpdate = false;
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            lock (_lockObject)
            {
                // First, mark items as expired but don't remove them yet
                foreach (var item in _conveyorBeltItems)
                {
                    var elapsed = currentTime - item.StartTime;
                    var wasExpired = item.IsExpired;
                    item.IsExpired = elapsed >= _itemDurationMs;
                    
                    if (!wasExpired && item.IsExpired)
                    {
                        Console.WriteLine($"[ScoreBanner] Item {item.ItemId} ({item.Score.HomeTeam} vs {item.Score.AwayTeam}) marked as expired");
                        needsUpdate = true;
                    }
                }

                // Remove items that have been expired for a while (cleanup buffer)
                var itemsToRemove = _conveyorBeltItems.Where(item =>
                {
                    if (!item.IsExpired) return false;
                    var elapsed = currentTime - item.StartTime;
                    return elapsed >= (_itemDurationMs + _cleanupBufferMs);
                }).ToList();

                if (itemsToRemove.Count > 0)
                {
                    foreach (var item in itemsToRemove)
                    {
                        Console.WriteLine($"[ScoreBanner] Item {item.ItemId} ({item.Score.HomeTeam} vs {item.Score.AwayTeam}) removed from memory after cleanup buffer");
                        _conveyorBeltItems.Remove(item);
                    }
                    needsUpdate = true;
                }

                // Add new items to the belt if conditions are met
                var timeSinceLastStart = currentTime - _lastItemStartTime;
                var canAddNewItem = _incomingScores.Count > 0 && 
                    (_lastItemStartTime == 0 || timeSinceLastStart >= _itemSpacingMs);

                if (canAddNewItem)
                {
                    var nextScore = _incomingScores.Dequeue();
                    var newItem = new ConveyorBeltItem
                    {
                        Score = nextScore,
                        StartTime = currentTime,
                        ItemId = ++_nextItemId,
                        IsExpired = false
                    };
                    
                    _conveyorBeltItems.Add(newItem);
                    _lastItemStartTime = currentTime;
                    needsUpdate = true;
                    
                    Console.WriteLine($"[ScoreBanner] Item {newItem.ItemId} ({newItem.Score.HomeTeam} vs {newItem.Score.AwayTeam}) added to belt. Total items: {_conveyorBeltItems.Count}, Active: {_conveyorBeltItems.Count(i => !i.IsExpired)}");
                }
            }

            // Update UI only if there were actual changes
            if (needsUpdate)
            {
                InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// Gets all items that should currently be visible on the conveyor belt
        /// Only returns items that haven't been marked as expired yet
        /// </summary>
        private IEnumerable<ConveyorBeltItem> GetActiveItems()
        {
            lock (_lockObject)
            {
                var activeItems = _conveyorBeltItems.Where(item => !item.IsExpired)
                                                  .OrderBy(item => item.StartTime)
                                                  .ToList(); // ToList() to avoid lock issues with deferred execution
                                                  
                // Debug: Occasional logging of active items
                if (_nextItemId % 10 == 0 && activeItems.Count > 0)
                {
                    Console.WriteLine($"[ScoreBanner] Active items: [{string.Join(", ", activeItems.Select(i => $"{i.ItemId}:{i.Score.HomeTeam}"))}]");
                }
                
                return activeItems;
            }
        }

        /// <summary>
        /// Calculates the animation delay for a conveyor belt item based on when it should have started
        /// </summary>
        private int GetAnimationDelay(ConveyorBeltItem item)
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var elapsed = currentTime - item.StartTime;
            // Return 0 if the animation should have already started (prevents negative delays)
            return (int)Math.Max(0, -elapsed);
        }

        public async ValueTask DisposeAsync()
        {
            if (_scoreSubscriptionId.HasValue)
            {
                SseClient.Unsubscribe(_messageType, _scoreSubscriptionId.Value);
                _scoreSubscriptionId = null;
            }

            _conveyorTimer?.Dispose();

            if (_jsModule is not null)
            {
                await _jsModule.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Represents a score item on the conveyor belt system
    /// Each item maintains its identity and state throughout its entire journey
    /// </summary>
    public class ConveyorBeltItem
    {
        public ScoreModel Score { get; set; }
        public long StartTime { get; set; } // When this item started its journey
        public int ItemId { get; set; } // Unique identifier for DOM tracking and CSS animations
        public bool IsExpired { get; set; } // Whether this item has finished its animation
    }
}
