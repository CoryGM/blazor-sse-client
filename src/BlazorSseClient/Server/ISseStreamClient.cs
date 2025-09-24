namespace BlazorSseClient.Server
{
    public interface ISseStreamClient
    {
        Task<Stream> GetSseStreamAsync(string urlOrPath, CancellationToken cancellationToken);
    }
}
