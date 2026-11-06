using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class DroneImageUploadFunction
    {
        private readonly TableServiceClient _tableService;
        private readonly ILogger<DroneImageUploadFunction> _logger;

        public DroneImageUploadFunction(
            TableServiceClient tableService,
            ILogger<DroneImageUploadFunction> logger)
        {
            _tableService = tableService;
            _logger = logger;
        }

        [Function("DroneImageUploadFunction")]
        public async Task Run(
            [BlobTrigger("drone-images/{name}",
                Connection = "AzureWebJobsStorage")] Stream blob,
            string name)
        {
            _logger.LogInformation(
                "Drone image uploaded: {ImageName}", name);

            // Create the audit table if it does not exist
            var table = _tableService.GetTableClient(
                "DroneProcessingAudit");

            await table.CreateIfNotExistsAsync();

            // Create an audit record
            var auditRecord = new TableEntity(
                DateTime.UtcNow.ToString("yyyyMMdd"),
                Guid.NewGuid().ToString())
            {
                { "ImageName", name },
                { "FileSizeBytes", blob.Length },
                { "ProcessingStatus", "Uploaded" },
                { "UploadedAtUtc", DateTime.UtcNow }
            };

            await table.AddEntityAsync(auditRecord);

            _logger.LogInformation(
                "Audit record saved for {ImageName}. Size: {FileSize} bytes",
                name, blob.Length);
        }
    }
}