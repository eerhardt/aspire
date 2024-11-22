using OrderProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureServiceBusClient("messaging");
builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
