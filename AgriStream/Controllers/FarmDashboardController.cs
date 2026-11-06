using AgriStream.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Controllers
{
    public class FarmDashboardController : Controller
    {
        private readonly FunctionsApiService _functionsApi;

        public FarmDashboardController(
            FunctionsApiService functionsApi)
        {
            _functionsApi = functionsApi;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var telemetry =
                    await _functionsApi.GetLatestTelemetryAsync();

                ViewBag.Telemetry = telemetry;
            }
            catch (Exception ex)
            {
                ViewBag.TelemetryError =
                    "Unable to retrieve telemetry: " + ex.Message;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerIrrigation(string deviceId)
        {
            try
            {
                var request = new IrrigationRequest
                {
                    DeviceId = deviceId,
                    Command = "StartIrrigation"
                };

                await _functionsApi.TriggerIrrigationAsync(request);

                TempData["Message"] =
                    "Irrigation command successfully submitted.";

            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to submit irrigation command: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}