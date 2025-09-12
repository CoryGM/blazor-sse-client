namespace BlazorSseClient.Demo.Api.Queues
{
    public record struct QueueMessage
    {
        public required string Type { get; init; }
        public required int Version { get; init; }
        public required string Payload { get; init; }
    }
}
