using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaychexReceiptOCR.Models;
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
                var RelativeImagePath = ImagePath + receipts[0].FileName;
                AbsImagePath = Path.Combine(wwwrootPath, RelativeImagePath);

                // Stores the receipt in wwwroot\userReceipts
                using (var fileStream = new FileStream(AbsImagePath, FileMode.Create))
                {
                    receipts[0].CopyTo(fileStream);
                }
            }

            // Gives the path to the OCRRead method
            return OCRRead(AbsImagePath, receipts[0].FileName);
        }

        [HttpPost]
        public IActionResult OCRRead(string filePath, string fileName)
        {
            // User receipt model used to store the data which is passed to the view
            Receipt model = new Receipt();
            model.Name = fileName;

            // Used to locate the tessdata folder
            string contentRootPath = _env.ContentRootPath;

            // Holds the text data read from tesseract
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
                            // Adds data to model
                            model.RawText = page.GetText();
                            model.MeanConfidence = page.GetMeanConfidence();

                            // Redirects console ouput to a string
                            var sw = new StringWriter();
                            Console.SetOut(sw);
                            Console.SetError(sw);

                            // Iterates through the tesseract page  
                            using (var iter = page.GetIterator())
                            {
                                iter.Begin();

                                do
                                {
                                    do
                                    {
                                        do
                                        {
                                            do
                                            {
                                                if (iter.IsAtBeginningOf(PageIteratorLevel.Block))
                                                {
                                                    // Whenever a new BLOCK it iterated, the current StringWriter contents are added to the the ouput
                                                    // and a new StringWriter object in instantiated in place of the old one
                                                    output.Add(sw.ToString());
                                                    sw = new StringWriter();
                                                    Console.SetOut(sw);
                                                    Console.SetError(sw);
                                                }

                                                Console.Write(iter.GetText(PageIteratorLevel.Word));
                                                Console.Write(" ");

                                                if (iter.IsAtFinalOf(PageIteratorLevel.TextLine, PageIteratorLevel.Word))
                                                {
                                                    Console.WriteLine("");
                                                }
                                            } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));

                                            if (iter.IsAtFinalOf(PageIteratorLevel.Para, PageIteratorLevel.TextLine))
                                            {
                                                Console.WriteLine("");
                                            }
                                        } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                                    } while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                                } while (iter.Next(PageIteratorLevel.Block));
                            }

                            output.Add(sw.ToString());
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

            // Add iterated text to model 
            model.IteratedText = output;

            // Passes model to Post view
            return View(model);
        }
    }
}
