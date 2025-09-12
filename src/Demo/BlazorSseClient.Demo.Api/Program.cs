using BlazorSseClient.Demo.Api.Queues;
using BlazorSseClient.Demo.Api.SportsScores.Background;
using BlazorSseClient.Demo.Api.SportsScores.Data;
using BlazorSseClient.Demo.Api.Stock.Background;
using BlazorSseClient.Demo.Api.Stocks.Data;
using BlazorSseClient.Demo.Api.Weather.Background;
using BlazorSseClient.Demo.Api.Weather.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddSingleton<IWeatherService, WeatherService>();
builder.Services.AddSingleton<IStockService, StockService>();
builder.Services.AddSingleton<ISportsScoreService, SportsScoreService>();
builder.Services.AddSingleton<MessageQueueService>();
builder.Services.AddHostedService<SportsScoreBackgroundService>();
builder.Services.AddHostedService<WeatherBackgroundService>();
builder.Services.AddHostedService<StockBackgroundService>();

builder.Services.AddHttpClient("WeatherApi", client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
});

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
