using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Controllers
{
    public class DroneImagesController : Controller
    {
        private readonly BlobServiceClient _blobService;

        public DroneImagesController(BlobServiceClient blobService)
        {
            _blobService = blobService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                TempData["Error"] = "Please select an image.";
                return RedirectToAction(nameof(Index));
            }

            string[] allowedExtensions =
                { ".jpg", ".jpeg", ".png", ".tif", ".tiff" };

            string extension =
                Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only JPG, PNG and TIFF images are allowed.";
                return RedirectToAction(nameof(Index));
            }

            if (image.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "The image must be smaller than 10 MB.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var container = _blobService.GetBlobContainerClient(
                    "drone-images");

                string fileName = $"{Guid.NewGuid()}{extension}";

                var blob = container.GetBlobClient(fileName);

                await using var stream = image.OpenReadStream();

                await blob.UploadAsync(stream, overwrite: false);

                TempData["Message"] =
                    "Drone image uploaded successfully. Processing has been triggered.";
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "Image upload failed. Please check the storage connection.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}