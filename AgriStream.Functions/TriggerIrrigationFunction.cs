using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class TriggerIrrigationFunction
    {
        private readonly QueueServiceClient _queueService;
        private readonly ILogger<TriggerIrrigationFunction> _logger;

        public TriggerIrrigationFunction(
            QueueServiceClient queueService,
            ILogger<TriggerIrrigationFunction> logger)
        {
            _queueService = queueService;
            _logger = logger;
        }

        [Function("TriggerIrrigation")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post",
                Route = "irrigation/trigger")] HttpRequestData req)
        {
            var command = await JsonSerializer.DeserializeAsync<IrrigationCommand>(
                req.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (command == null ||
                string.IsNullOrWhiteSpace(command.DeviceId) ||
                string.IsNullOrWhiteSpace(command.Command))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);

                await badRequest.WriteStringAsync(
                    "DeviceId and Command are required.");

                return badRequest;
            }

            command.RequestedAtUtc = DateTime.UtcNow;

            var queue = _queueService.GetQueueClient(
                "irrigation-commands");

            await queue.CreateIfNotExistsAsync();

            string message = JsonSerializer.Serialize(command);

            await queue.SendMessageAsync(message);

            _logger.LogInformation(
                "Irrigation command {Command} queued for device {DeviceId}.",
                command.Command, command.DeviceId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);

            await response.WriteAsJsonAsync(new
            {
                Message = "Irrigation command successfully added to the queue.",
                command.DeviceId,
                command.Command,
                command.RequestedAtUtc
            });

            return response;
        }

        public class IrrigationCommand
        {
            public string DeviceId { get; set; } = string.Empty;

            public string Command { get; set; } = string.Empty;

            public DateTime RequestedAtUtc { get; set; }
        }
    }
}