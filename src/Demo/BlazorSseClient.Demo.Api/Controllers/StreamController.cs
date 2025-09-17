using BlazorSseClient.Demo.Api.Queues;
using Microsoft.AspNetCore.Cors;
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
        [EnableCors("SseCorsPolicy")] 
        public async Task Stream(CancellationToken token)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");

            await foreach (var message in _queue.Subscribe(token))
            {
                await Response.WriteAsync($"id: {Guid.NewGuid()}\n\n", token);
                await Response.WriteAsync($"event: {message.Type}\n\n", token);
                await Response.WriteAsync($"data: {message.Payload}\n\n", token);
                await Response.Body.FlushAsync(token);
            }
        }
    }
}
