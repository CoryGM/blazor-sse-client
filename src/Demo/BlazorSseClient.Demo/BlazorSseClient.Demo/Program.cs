using BlazorSseClient.Demo.Components;
using BlazorSseClient.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient("Api",
    c =>
    {
        var baseAddress = builder.Configuration["Api:BaseAddress"];
        if (String.IsNullOrWhiteSpace(baseAddress))
        {
            throw new InvalidOperationException("Api:BaseAddress configuration value is missing or empty.");
        }
        c.BaseAddress = new Uri(baseAddress);
    });

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

builder.Services.AddServerSseClient(options =>
{
    options.BaseAddress = builder.Configuration["Sse:BaseAddress"];
    options.Timeout = Timeout.InfiniteTimeSpan;
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorSseClient.Demo.Client._Imports).Assembly);

app.Run();
