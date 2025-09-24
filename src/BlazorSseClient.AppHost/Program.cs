var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BlazorSseClient_Demo>("blazorsseclient-demo");

builder.AddProject<Projects.BlazorSseClient_Demo_Api>("blazorsseclient-demo-api");

builder.Build().Run();
