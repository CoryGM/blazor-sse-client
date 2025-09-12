using BlazorSseClient.Demo.Api.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlazorSseClient.Demo.Api.Controllers
{
    [Route("api/stream")]
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

            while (!token.IsCancellationRequested)
            {
                var message = await _queue.DequeueAsync(token);

                if (message is not null)
                {
                    await Response.WriteAsync($"id: {Guid.NewGuid()}\n\n", token);
                    await Response.WriteAsync($"event: {message.Value.Type}\n\n", token);
                    await Response.WriteAsync($"data: {message.Value.Payload}\n\n", token);
                    await Response.Body.FlushAsync(token);
                }
            }
        }

    }
}
