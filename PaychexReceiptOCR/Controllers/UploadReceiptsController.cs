using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaychexReceiptOCR.Models;
using System.Threading.Tasks;
using PaychexReceiptOCR.Helpers;
using System;
using System.Diagnostics;

namespace PaychexReceiptOCR.Controllers
{
    public class UploadReceiptsController : Controller
    {
        private readonly IWebHostEnvironment _env;

        // Used for Progress Bar
        private double receiptsProcessed = 0;

        public UploadReceiptsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult FileUploader()
        {
            CleanUserReceipts();
            return View();
        }

        public IActionResult FolderUploader()
        {
            CleanUserReceipts();
            return View();
        }

        /// <summary>
        ///     Recieves the uploaded files from the view, processes each one in parallel,
        ///     then returns all processed receipts to the view.
        /// </summary>
        [HttpPost("UploadReceipts")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> PostAsync()
        {
            // Finds path to wwwroot
            string wwwrootPath = _env.WebRootPath;

            // Recieves files uploaded from the form
            var uploads = HttpContext.Request.Form.Files;

            // Used for progress bar 
            double totalReceipts = uploads.Count;

            // Holds the collection of tasks
            List<Task<Receipt>> createReceiptTasks = new List<Task<Receipt>>();

            // Processes each IFormFile in parallel
            foreach (var upload in uploads)
            {
                createReceiptTasks.Add(CreateReceiptAsync(upload, wwwrootPath, totalReceipts));
            }

            // A List<Receipt> object that is returned when all the tasks are complete
            var receipts = await Task.WhenAll(createReceiptTasks);

            // Gives the receipts list to the View
            return View(receipts);
        }

        /// <summary>
        ///     Recieves the IFormFile and returns a receipt object from it which contains an OCR reading 
        ///     and a Regex analysis of key data points.
        /// </summary>
        private async Task<Receipt> CreateReceiptAsync(IFormFile image, string rootPath, double totalReceipts)
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

            if (!System.String.IsNullOrWhiteSpace(readReceipt.RawText)) {
                // Identifys the Vender
                readReceipt.Vendor = RegexMethods.IdentifyVendor(readReceipt.RawText, contentRootPath);

                // Finds the date and cost
                RegexMethods.FindDateAndCost(readReceipt, contentRootPath);
            }

            // Increments the receipts processed and stores the percentage of completion of all receipts in the
            // text file which can be read by the Status() method
            receiptsProcessed++;
            string percent = Convert.ToInt32((receiptsProcessed / totalReceipts) * 100).ToString();

            // Catches possibility of exception being thrown because multiple asynchronous methods 
            // may attempt to write or read from the file at the same time.
            try
            {
                if (totalReceipts - receiptsProcessed == 1)
                {
                    System.IO.File.WriteAllText(Path.Combine(contentRootPath + "\\Properties\\Log\\log.txt"), "100");
                }
                else if (!(totalReceipts - receiptsProcessed == 0))
                {
                    System.IO.File.WriteAllText(Path.Combine(contentRootPath + "\\Properties\\Log\\log.txt"), percent);
                }
                else
                {
                    System.IO.File.WriteAllText(Path.Combine(contentRootPath + "\\Properties\\Log\\log.txt"), "0");
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Debug.Write("Unexpected Error: " + e.Message);
                Debug.Write("Details: ");
                Debug.Write(e.ToString());
            };

            return readReceipt;
        }

        /// <summary>
        ///     Checks the \\Log\\log.txt file for the current percentage of completion of the receipt processing.
        /// </summary>
        public JsonResult Status()
        {
            string contentRootPath = _env.ContentRootPath;
            string text = System.IO.File.ReadAllText(Path.Combine(contentRootPath + "\\Properties\\Log\\log.txt"));
            string percent = text + '%';
            return Json(percent);
        }

        /// <summary>
        ///     Clears out all leftover files in the userReceipts folder from previous iterations of program .
        /// </summary>
        public void CleanUserReceipts()
        {
            // Finds path to wwwroot
            string wwwrootPath = _env.WebRootPath;

            System.IO.DirectoryInfo userReceipts = new DirectoryInfo(wwwrootPath + "\\userReceipts");

            foreach (FileInfo file in userReceipts.GetFiles())
            {
                // This is a placeholder receipt which is kept in userReceipts to prevent GitHub from deleting 
                // the folder since it is empty before the program is run.
                if(file.Name != "Delta.png")
                {
                    file.Delete();
                }
            }
        }
    }
}
