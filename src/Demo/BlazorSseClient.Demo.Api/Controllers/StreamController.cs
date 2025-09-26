using BlazorSseClient.Demo.Api.Queues;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace BlazorSseClient.Demo.Api.Controllers
{
    [Route("stream")]
    [EnableCors("SseCorsPolicy")]
    [ApiController]
    public class StreamController : ControllerBase
    {
        private readonly MessageQueueService _queue;

        public StreamController(MessageQueueService queue)
        {
            _queue = queue;
        }

        [HttpGet]
        [Route("messages")]
        public async Task Stream(CancellationToken token)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("X-Accel-Buffering", "no"); // if behind nginx, disable buffering

            await foreach (var message in _queue.Subscribe(token))
            {
                var id = Guid.NewGuid();

                // Build the event with single-line terminations and one blank line to end the event
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"id: {id}");
                sb.AppendLine($"event: {message.Type}");

                // Ensure multi-line payloads are sent correctly
                foreach (var line in (message.Payload ?? String.Empty).Split('\n'))
                {
                    sb.AppendLine($"data: {line.TrimEnd('\r')}");
                }

                sb.AppendLine(); // blank line terminates the event

                var eventText = sb.ToString();
                await Response.WriteAsync(eventText, token);
                await Response.Body.FlushAsync(token);
            }
        }
    }
}
