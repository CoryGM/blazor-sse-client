# blazor-sse-client
Server-Sent Events support for Blazor WASM via JS interop and Server via 
native .NET capabilities. 

# Server Sent Events
Server Sent Events (SSE) is a standard allowing servers to push events to
clients over HTTP. It is a simpler alternative to WebSockets for many use cases,
including real-time notifications, live updates, and streaming data.
SSE is natively supported in modern browsers via the EventSource API, and
can be easily integrated into Blazor applications using JavaScript interop.
This library provides a simple and efficient way to use SSE in Blazor
applications, both on the client-side (Blazor WebAssembly) and server-side
(Blazor Server).

# Features
- Support for both Blazor WebAssembly and Blazor Server
- Automatic reconnection and error handling
- Lightweight and efficient
- Simple API for subscribing to events and handling messages
- No external dependencies
- Cross-platform support (works on any platform that supports Blazor)
- Supports cancellation and timeouts
- Thread-safe and scalable
- Supports multiple event sources
- Supports both synchronous and asynchronous event handling

# Installation
You can install the library via NuGet:
```
dotnet add package BlazorSseClient
```
Or via the NuGet Package Manager in Visual Studio.
```
Install-Package BlazorSseClient
```
# Setup
## Blazor WebAssembly
To use the library in a Blazor WebAssembly application, follow these steps:
1. Register the `SseClient` service in your `Program.cs` file:
```csharp
builder.Services.AddWasmSseClient(options =>
{
    options.BaseAddress = builder.Configuration["Sse:BaseAddress"];
    options.ReconnectBaseDelayMs = 1000;
    options.ReconnectMaxDelayMs = 15000;
    options.UseCredentials = false;
    options.AutoStart = true;
});
```

If you have a hybrid application with the Layout.razor and App.Razor in 
the server project you will need to set the AutoStart property
to true on the Wasm client because there isn't really a good place to 
hook in a Start() call. 

## Blazor Server
To use the library in a Blazor Server application, follow these steps:
1. Register the `SseClient` service in your `Program.cs` file:
```csharp
builder.Services.AddServerSseClient(options =>
{
    options.BaseAddress = builder.Configuration["Sse:BaseAddress"];
    options.Timeout = Timeout.InfiniteTimeSpan;
});
```

2. Start listening for events in the `App.razor` file. 

```csharp
 [Inject]
 private ISseClient SseClient { get; set; } = default!;
 
 protected override async Task OnInitializedAsync()
 {
     await SseClient.StartAsync().ConfigureAwait(false);
 }
```

# Usage
Usage is the same for both Blazor WebAssembly and Blazor Server. 

1. Implement IAsyncDisposable in your component:
```csharp
@implements IAsyncDisposable
```

2. Inject the `ISseClient` service into your component:
```csharp
@inject ISseClient SseClient
```


3. Use the `SseClient` to connect to an SSE endpoint and handle events:
```csharp
	
 @code {
     private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
     private const string _messageType = "Score";
     private Guid? _scoreSubscriptionId;

     // Has to be setup in OnAfterRenderAsync() or OnAfterRender() 
     // to ensure JS interop is ready and to avoid intermediate disposals
     // during component initialization.
     protected override async Task OnAfterRenderAsync(bool firstRender)
     {
         if (firstRender)
         {
            _scoreSubscriptionId = SseClient.Subscribe(_messageType, ScoreCallback);
         }
     }

     // Synchronous callback for SseClient.Subscribe()
     // This method should just do the minimal work needed to
     // handle the incoming event and capture the data. What is
     // done here should be fast and non-blocking. Real work 
     // should be done outside this method.
     private void ScoreCallback(SseEvent sseEvent)
     {
         try
         {
             var score = JsonSerializer.Deserialize<ScoreModel?>(sseEvent.Data, _jsonOptions);

             if (score is null)
                 return;

             // Perform work using the data 
             // Ideally this would be done in a separate method that
             // would cleanly encapsulate the work.
             PerformAddScoreWork();
         }
         catch (JsonException)
         {
             // Log or handle the error as needed
             return;
         }

         Console.WriteLine($"Before StateHasChanged. readings.Count: {_readings.Count}");
         InvokeAsync(StateHasChanged);
         Console.WriteLine("After StateHasChanged");
     }

     // Real work done outside the SseClient.Subscribe() callback
     // Can be async if needed.
     private void PerformAddScoreWork(ScoreModel score)
     {
        
     }

     // Cleanup subscriptions 
     public async ValueTask DisposeAsync()
     {
         if (_weatherSubscriptionId.HasValue)
         {
             SseClient.Unsubscribe(_messageType, _scoreSubscriptionId.Value);
             _weatherSubscriptionId = null;
         }
     }
 }
```
An example of this working both for rendering on the server or 
rending in the browser can be seen in the demo application at 
on the [Scores page](https://localhost:7041/scores).

# Demo Project
The demo project is an Aspire application and the startup project
should be set to BlazorSseClient.AppHost.

It shows a fully working implementation of the library in 
server-only (Weather), browser-only (Stocks), and hybrid scenarios. 

The API project consists of three background services all sending 
data to a queue that is watched by the method Stream method on the
StreamController. This was done to show a somewhat realistic 
method of watching for events produced by external sources that need
to be streamed to the clients. 

```csharp
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
```

### Import Caveat for the Stream Controller and JS EventSource

Some examples for SSE projects will show each line of the output 
as a separate WriteAsync() for the id: and event: and data: lines. 

This approach will work for the native client in .NET because it
can correctly interpret the stream. It will NOT work for the JavaScript
Interop because the native JavaScript EventSource component does not
correctly interpret the stream when the components are send individually.

Therefor, if you are creating the API side make sure to follow the 
example in the API controller of building the string to send and then
sending it in a single WriteAsync();






