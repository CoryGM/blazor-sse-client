using BlazorSseClient.Services;

namespace BlazorSseClient.Server
{
    public class ServerSseClient : ISseClient
    {
        public async ValueTask DisposeAsync()
        {
            return;
        }

        public async Task StartAsync(string url, bool restartOnDifferentUrl = true)
        {
            Console.WriteLine($"ServerSseClient: StartAsync called with URL: {url}");
        }

        public async Task StopAsync()
        {
            Console.WriteLine("ServerSseClient: StopAsync called");
        }
    }
}
