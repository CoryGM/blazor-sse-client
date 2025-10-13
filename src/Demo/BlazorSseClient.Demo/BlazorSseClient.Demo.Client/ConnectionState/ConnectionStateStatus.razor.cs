using BlazorSseClient.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorSseClient.Demo.Client.ConnectionState
{
    public partial class ConnectionStateStatus : ComponentBase, IAsyncDisposable
    {
        private string _renderLocation = String.Empty;
        private Guid? _subscriptionId = null;
        private string _currentConnectionState = "Unknown";   
        private string _currentLifecycleState = "Unknown";
        private Timer? _connectionStateTimer = null;

        [Inject]
        private ISseClient SseClient { get; set; } = null!;

        protected override void OnInitialized()
        {
            if (OperatingSystem.IsBrowser())
                _renderLocation = "Browser";
            else
                _renderLocation = "Server";
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                _subscriptionId = SseClient.SubscribeConnectionStateChange(ConnectionStateChanged);

                // Start timer to check connection state every 5 seconds
                _connectionStateTimer = new Timer(CheckConnectionStateTimer, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            }
        }

        private void CheckConnectionStateTimer(object? state)
        {
            CheckConnectionState();
        }

        private void CheckConnectionState()
        {
            var newConnectionState = SseClient.ConnectionState.ToString();

            if (_currentConnectionState != newConnectionState)
            {
                _currentConnectionState = newConnectionState;

                // Ensure UI update happens on the correct thread
                InvokeAsync(StateHasChanged);
            }
        }

        private void ConnectionStateChanged(SseEvent sseEvent)
        {
            _currentLifecycleState = sseEvent.Data;
            CheckConnectionState();
        }

        public async ValueTask DisposeAsync()
        {
            if (_subscriptionId.HasValue)
            {
                SseClient.UnsubscribeConnectionStateChange(_subscriptionId.Value);
                _subscriptionId = null;
            }

            if (_connectionStateTimer is not null)
            {
                await _connectionStateTimer.DisposeAsync();
                _connectionStateTimer = null;
            }
        }
    }
}
