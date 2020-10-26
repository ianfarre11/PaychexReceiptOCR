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
using System.Text.RegularExpressions;

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

            // Recieves files uploaded from the form
            var uploads = HttpContext.Request.Form.Files;

            List<Receipt> receipts = new List<Receipt>();

            if (uploads.Count != 0)
            {
                // Creates a new Receipt instance for each uploaded file
                // Adds all these new Receipts to the receipts List
                foreach (var upload in uploads)
                {
                    Receipt newReceipt = new Receipt();
                    newReceipt.Name = upload.FileName;

                    // Creates a path to wwwroot\userReceipts for the image to be stored
                    var ImagePath = @"userReceipts\";
                    var RelativeImagePath = ImagePath + upload.FileName;
                    var AbsImagePath = Path.Combine(wwwrootPath, RelativeImagePath);

                    newReceipt.Path = AbsImagePath;

                    // Stores the image file in wwwroot\userReceipts
                    using (var fileStream = new FileStream(AbsImagePath, FileMode.Create))
                    {
                        upload.CopyTo(fileStream);
                    }

                    receipts.Add(newReceipt);
                }
            }

            // Gives the receipts list to the OCRRead method
            return OCRRead(receipts);
        }

        [HttpPost]
        public IActionResult OCRRead(List<Receipt> model)
        {

            // Used to locate the tessdata folder
            string contentRootPath = _env.ContentRootPath;

            // Iterates through the receipts and reads the associated images
            // Stores the readings in the receipts variables
            foreach (Receipt receipt in model)
            {
                // Holds the iterated text data read from tesseract
                List<string> output = new List<string>();

                try
                {
                    // Creates engine
                    using (var engine = new TesseractEngine(contentRootPath, "eng", EngineMode.Default))
                    {
                        // Loads receipt as Tesseract.Pix instance
                        using (var img = Pix.LoadFromFile(receipt.Path))
                        {
                            // Reads receipt
                            using (var page = engine.Process(img))
                            {
                                // Adds reading to receipt
                                receipt.RawText = page.GetText();
                                receipt.MeanConfidence = page.GetMeanConfidence();

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

                // Add iterated text to receipt model
                receipt.IteratedText = output;

                // Demo of Basic Regex Parsing
                Regex rxTotalCost = new Regex(@"(?<=Total Price: )\S+");
                receipt.TotalCost = rxTotalCost.Match(receipt.RawText).ToString();

                Regex rxDate = new Regex(@"(?<=Date of Purchase: )\w+\s\d+\W\s\d+");
                receipt.Date = rxDate.Match(receipt.RawText).ToString();


                // Regex rxCard = new Regex(@"(?<=Date of Purchase: )\S+");
                // receipt.Date = rxDate.Match(receipt.RawText).ToString();

                Regex rxTicketNumber = new Regex(@"(?<=Ti[c(]ket Number: )\d+");
                receipt.TicketNumber = rxTicketNumber.Match(receipt.RawText).ToString();
            }



            // Passes List of receipts to Post view
            return View(model);
        }

        // Identifys if receipt is from Starbucks, Walmart, WaffleHouse, or other 
        static string IdentifyVendor(string rawText)
        {

            return ("Please Compile");
        }
    }
}
