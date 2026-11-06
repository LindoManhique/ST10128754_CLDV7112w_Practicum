using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class GetLatestTelemetryFunction
    {
        private readonly TableServiceClient _tableService;
        private readonly ILogger<GetLatestTelemetryFunction> _logger;

        public GetLatestTelemetryFunction(
            TableServiceClient tableService,
            ILogger<GetLatestTelemetryFunction> logger)
        {
            _tableService = tableService;
            _logger = logger;
        }

        [Function("GetLatestTelemetry")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
                Route = "telemetry/latest")] HttpRequestData req)
        {
            _logger.LogInformation(
                "Retrieving latest farm telemetry readings.");

            var table = _tableService.GetTableClient("FarmTelemetry");

            var readings = new List<TableEntity>();

            await foreach (var entity in table.QueryAsync<TableEntity>(
                maxPerPage: 100))
            {
                readings.Add(entity);
            }

            var latestReadings = readings
                .OrderByDescending(x =>
                    x.Timestamp ?? DateTimeOffset.MinValue)
                .Take(10)
                .ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(new
            {
                Message = "Latest farm telemetry retrieved successfully.",
                TotalReadings = latestReadings.Count,
                Readings = latestReadings
            });

            return response;
        }
    }
}