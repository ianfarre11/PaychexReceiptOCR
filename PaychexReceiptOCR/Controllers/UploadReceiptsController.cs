using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tesseract;

namespace PaychexReceiptOCR.Controllers
{
    public class UploadReceiptsController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public UploadReceiptsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Read()
        {
            return View();
        }

        [HttpPost("UploadReceipts")]
        public IActionResult Post()
        {
            // Finds path to wwwroot
            string wwwrootPath = _env.WebRootPath;

            // Recieves receipt from View
            // Currently set up to only process one receipt at a time
            var receipts = HttpContext.Request.Form.Files;

            var AbsImagePath = "";

            if (receipts.Count != 0)
            {
                // Creates a path to wwwroot\userReceipts for the receipt to be stored
                var ImagePath = @"userReceipts\";
                var Extension = Path.GetExtension(receipts[0].FileName);
                var RelativeImagePath = ImagePath + receipts[0].FileName + Extension;
                AbsImagePath = Path.Combine(wwwrootPath, RelativeImagePath);

                // Stores the receipt in wwwroot\userReceipts
                using (var fileStream=new FileStream(AbsImagePath, FileMode.Create))
                {
                    receipts[0].CopyTo(fileStream);
                }
            }

            // Gives the path to the OCRRead method
            return OCRRead(AbsImagePath);
        }

        [HttpPost]
        public IActionResult OCRRead(string filePath)
        {
            // Used to locate the tessdata folder
            string contentRootPath = _env.ContentRootPath;

            // Holds the mean confidence and text data read from tesseract
            List<string> output = new List<string>();
            try
            {
                // Creates engine
                using (var engine = new TesseractEngine(contentRootPath, "eng", EngineMode.Default))
                {
                    // Loads receipt as Tesseract.Pix instance
                    using (var img = Pix.LoadFromFile(filePath))
                    {
                        // Reads receipt
                        using (var page = engine.Process(img))
                        {
                            // Adds reading to output
                            var text = page.GetText();
                            output.Add("Mean confidence: " + page.GetMeanConfidence());
                            output.Add("Text (GetText): " + text);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                output.Add("Unexpected Error: " + e.Message);
                output.Add("Details: ");
                output.Add(e.ToString());
            };
            
            // Stores reading in ViewBag to be read by View Object
            ViewBag.output = output;
            return View("Read");
        }
    }
}
