using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaychexReceiptOCR.Models;
using System.Threading.Tasks;
using PaychexReceiptOCR.Helpers;

namespace PaychexReceiptOCR.Controllers
{
    public class UploadReceiptsController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public UploadReceiptsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult FileUploader()
        {
            return View();
        }

        public IActionResult FolderUploader()
        {
            return View();
        }

        [HttpPost("UploadReceipts")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> PostAsync()
        {
            // Finds path to wwwroot
            string wwwrootPath = _env.WebRootPath;

            // Recieves files uploaded from the form
            var uploads = HttpContext.Request.Form.Files;

            // Holds the collection of tasks
            List<Task<Receipt>> createReceiptTasks = new List<Task<Receipt>>();

            // Processes each IFormFile in parallel
            foreach (var upload in uploads)
            {
                createReceiptTasks.Add(CreateReceiptAsync(upload, wwwrootPath));
            }

            // A List<Receipt> object that is returned when all the tasks are complete
            var receipts = await Task.WhenAll(createReceiptTasks);

            // Gives the receipts list to the View
            return View(receipts);
        }

        // Processes the IFormFile
        private async Task<Receipt> CreateReceiptAsync(IFormFile image, string rootPath)
        {
            // Finds path to wwwroot
            string contentRootPath = _env.ContentRootPath;

            Receipt newReceipt = new Receipt();
            newReceipt.Name = Path.GetFileName(image.FileName);

            // Creates a path to wwwroot\userReceipts for the image to be stored
            var ImagePath = @"userReceipts\";
            var RelativeImagePath = ImagePath + newReceipt.Name;
            var AbsImagePath = Path.Combine(rootPath, RelativeImagePath);

            newReceipt.Path = AbsImagePath;

            // Stores the image file in wwwroot\userReceipts
            using (var fileStream = new FileStream(AbsImagePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // Fixes orientation on image
            ImageProcessingMethods.ImageOrient(AbsImagePath);

            // Runs an OCRReading on the image and returns a new Receipt
            // with the reading data 
            var readReceipt = TesseractMethods.OCRRead(newReceipt, contentRootPath);

            // Identifys the Vender
            if (!System.String.IsNullOrWhiteSpace(readReceipt.RawText)) {
                readReceipt.Vendor = RegexMethods.IdentifyVendor(readReceipt.RawText, contentRootPath);
            }

            return readReceipt;
        }
    }
}
