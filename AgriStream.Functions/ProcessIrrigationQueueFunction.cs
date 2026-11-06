
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions
{
    public class ProcessIrrigationQueueFunction
    {
        private readonly ILogger<ProcessIrrigationQueueFunction> _logger;

        public ProcessIrrigationQueueFunction(
            ILogger<ProcessIrrigationQueueFunction> logger)
        {
            _logger = logger;
        }

        [Function("ProcessIrrigationQueue")]
        public async Task Run(
            [QueueTrigger("irrigation-commands",
                Connection = "AzureWebJobsStorage")] string queueMessage)
        {
            try
            {
                _logger.LogInformation(
                    "Processing irrigation command: {Message}", queueMessage);

                var command = JsonSerializer.Deserialize<IrrigationCommand>(
                    queueMessage,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

                if (command == null ||
                    string.IsNullOrWhiteSpace(command.DeviceId) ||
                    string.IsNullOrWhiteSpace(command.Command))
                {
                    throw new InvalidOperationException(
                        "Invalid irrigation command received.");
                }

                string connectionString =
                    Environment.GetEnvironmentVariable("SqlConnection")
                    ?? throw new InvalidOperationException(
                        "SQL connection string is missing.");

                string status = command.Command.ToLowerInvariant() switch
                {
                    "startirrigation" => "Irrigating",
                    "stopirrigation" => "Idle",
                    _ => "CommandReceived"
                };

                await using var connection =
                    new SqlConnection(connectionString);

                await connection.OpenAsync();

                _logger.LogInformation(
                    "Successfully connected to Azure SQL Database.");

                string sql = @"
                    UPDATE dbo.DeviceRegistry
                    SET OperationalStatus = @Status,
                        LastCommand = @Command,
                        LastUpdatedUtc = @UpdatedAt
                    WHERE DeviceId = @DeviceId;

                    IF @@ROWCOUNT = 0
                    BEGIN
                        INSERT INTO dbo.DeviceRegistry
                        (DeviceId, OperationalStatus, LastCommand, LastUpdatedUtc)
                        VALUES
                        (@DeviceId, @Status, @Command, @UpdatedAt);
                    END;
                ";

                await using var sqlCommand =
                    new SqlCommand(sql, connection);

                sqlCommand.Parameters.AddWithValue(
                    "@DeviceId", command.DeviceId);

                sqlCommand.Parameters.AddWithValue(
                    "@Status", status);

                sqlCommand.Parameters.AddWithValue(
                    "@Command", command.Command);

                sqlCommand.Parameters.AddWithValue(
                    "@UpdatedAt", DateTime.UtcNow);

                await sqlCommand.ExecuteNonQueryAsync();

                _logger.LogInformation(
                    "Device {DeviceId} updated to status {Status}.",
                    command.DeviceId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "FAILED to process irrigation queue message: {Message}",
                    queueMessage);

                throw;
            }
        }

        public class IrrigationCommand
        {
            public string DeviceId { get; set; } = string.Empty;

            public string Command { get; set; } = string.Empty;

            public DateTime RequestedAtUtc { get; set; }
        }
    }
}
