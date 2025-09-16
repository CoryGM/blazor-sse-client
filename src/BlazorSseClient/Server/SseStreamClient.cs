namespace BlazorSseClient.Server;

public sealed class SseStreamClient : ISseStreamClient
{
    private readonly HttpClient _http;

    public SseStreamClient(HttpClient http)
    {
        _http = http;
    }

    public Task<Stream> GetSseStreamAsync(string urlOrPath, CancellationToken cancellationToken)
        => _http.GetStreamAsync(urlOrPath, cancellationToken);
}