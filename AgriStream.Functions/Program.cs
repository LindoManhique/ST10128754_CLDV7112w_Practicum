using Azure.Data.Tables;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Register Azure Table Storage
builder.Services.AddSingleton<TableServiceClient>(_ =>
{
    var connectionString =
        Environment.GetEnvironmentVariable("AzureWebJobsStorage");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException(
            "Azure Storage connection string is missing.");
    }

    return new TableServiceClient(connectionString);
});

// Application Insights
if (!string.IsNullOrEmpty(
    Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

// Register Azure Queue Storage
var storageConnectionString =
    Environment.GetEnvironmentVariable("AzureWebJobsStorage");

if (string.IsNullOrWhiteSpace(storageConnectionString))
{
    throw new InvalidOperationException(
        "Azure Storage connection string is missing.");
}

builder.Services.AddSingleton<QueueServiceClient>(
    new QueueServiceClient(storageConnectionString));

builder.Build().Run();