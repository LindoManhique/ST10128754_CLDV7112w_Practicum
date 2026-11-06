using System.Net.Http.Json;

namespace AgriStream.Services
{
    public class FunctionsApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public FunctionsApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> GetLatestTelemetryAsync()
        {
            var client = _httpClientFactory.CreateClient(
                "AgriStreamFunctions");

            var response = await client.GetAsync("telemetry/latest");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> TriggerIrrigationAsync(
            IrrigationRequest request)
        {
            var client = _httpClientFactory.CreateClient(
                "AgriStreamFunctions");

            var response = await client.PostAsJsonAsync(
                "irrigation/trigger", request);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }

    public class IrrigationRequest
    {
        public string DeviceId { get; set; } = string.Empty;

        public string Command { get; set; } = string.Empty;
    }
}