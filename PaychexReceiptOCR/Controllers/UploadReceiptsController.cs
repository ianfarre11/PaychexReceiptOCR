using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaychexReceiptOCR.Models;
using Tesseract;
using System.Text.RegularExpressions;
using ImageMagick;
using System.Threading.Tasks;

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
            Receipt newReceipt = new Receipt();
            newReceipt.Name = image.FileName;

            // Creates a path to wwwroot\userReceipts for the image to be stored
            var ImagePath = @"userReceipts\";
            var RelativeImagePath = ImagePath + image.FileName;
            var AbsImagePath = Path.Combine(rootPath, RelativeImagePath);

            newReceipt.Path = AbsImagePath;

            // Stores the image file in wwwroot\userReceipts
            using (var fileStream = new FileStream(AbsImagePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // Fixes orientation on images
            ImageOrient(AbsImagePath);

            // Runs an OCRReading on the image and returns a new Receipt
            // with the reading data 
            var readReceipt = OCRRead(newReceipt);

            // Identifys the Vender
            readReceipt.Vendor = IdentifyVendor(readReceipt.RawText, rootPath);

            return readReceipt;
        }

        // Retrieves image file from the given path and fixes
        // potential orientation bug from images taken on a cellphone
        public void ImageOrient(string path)
        {
            try
            {
                // Read from file
                using MagickImage image = new MagickImage(path);
                image.AutoOrient();

                image.Format = MagickFormat.Png;
                // Save the result
                image.Write(path);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Debug.Write("Unexpected Error: " + e.Message);
                Debug.Write("Details: ");
                Debug.Write(e.ToString());
            };
        }

        private Receipt OCRRead(Receipt receipt)
        {
            // Used to locate the tessdata folder
            string contentRootPath = _env.ContentRootPath;

            try
            {
                // Creates engine
                using var engine = new TesseractEngine(contentRootPath, "eng", EngineMode.Default);
                // Loads receipt as Tesseract.Pix instance
                using var img = Pix.LoadFromFile(receipt.Path);
                // Reads receipt
                using var page = engine.Process(img);
                // Adds reading to receipt
                receipt.RawText = page.GetText();
                receipt.MeanConfidence = page.GetMeanConfidence();
                receipt.IteratedText = IteratePage(page);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Debug.Write("Unexpected Error: " + e.Message);
                Debug.Write("Details: ");
                Debug.Write(e.ToString());
            };

            // Passes List of receipts to Post view
            return receipt;
        }

        private List<string> IteratePage(Page page)
        {
            // Holds the iterated text data 
            List<string> iterated = new List<string>();

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
                                    iterated.Add(sw.ToString());
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

            iterated.Add(sw.ToString());

            return iterated;
        }

        // Identifies if receipt is from Starbucks, Walmart, WaffleHouse, or other 
        static string IdentifyVendor(string rawText, string contentRootPath)
        {
            int WaffleHouseCount = 0;
            int WalmartCount = 0;
            int StarbucksCount = 0;
            int SamsClubCount = 0;
            string[] RegexList;
            List<int> CountList = new List<int>();
            
            //Check for Walmart key expressions
            RegexList = System.IO.File.ReadAllLines(Path.Combine(contentRootPath + "\\..\\Properties\\Regex\\WalmartRegex.txt"));
            for (int i = 0; i < RegexList.Length; i++)
            {
                Regex rgx = new Regex(RegexList[i]);
                if (rgx.IsMatch(rawText))
                {
                    WalmartCount = WalmartCount + Int32.Parse(RegexList[i + 1]);
                }
                i++;
            }
            CountList.Add(WalmartCount);

            //Check for Waffle House key expressions
            RegexList = System.IO.File.ReadAllLines(Path.Combine(contentRootPath + "\\..\\Properties\\Regex\\WaffleHouseRegex.txt"));
            for (int i = 0; i < RegexList.Length; i++)
            {
                Regex rgx = new Regex(RegexList[i]);
                if (rgx.IsMatch(rawText))
                {
                    WaffleHouseCount = WaffleHouseCount + Int32.Parse(RegexList[i + 1]);
                }
                i++;
            }
            CountList.Add(WaffleHouseCount);

            //Check for Starbucks key expressions
            RegexList = System.IO.File.ReadAllLines(Path.Combine(contentRootPath + "\\..\\Properties\\Regex\\StarbucksRegex.txt"));
            for (int i = 0; i < RegexList.Length; i++)
            {
                Regex rgx = new Regex(RegexList[i]);
                if (rgx.IsMatch(rawText))
                {
                    StarbucksCount = StarbucksCount + Int32.Parse(RegexList[i + 1]);
                }
                i++;
            }
            CountList.Add(StarbucksCount);

            //Check for Sam's Club key expressions
            RegexList = System.IO.File.ReadAllLines(Path.Combine(contentRootPath + "\\..\\Properties\\Regex\\SamsClubRegex.txt"));
            for (int i = 0; i < RegexList.Length; i++)
            {
                Regex rgx = new Regex(RegexList[i]);
                if (rgx.IsMatch(rawText))
                {
                    SamsClubCount = SamsClubCount + Int32.Parse(RegexList[i + 1]);
                }
                i++;
            }
            CountList.Add(SamsClubCount);

            //Compare count totals and decide vendor
            int MaxCount = 0;
            foreach(int i in CountList)
            {
                MaxCount = Math.Max(MaxCount, i);
            }
            if (MaxCount == WaffleHouseCount && WaffleHouseCount != 0)
            {
                return ("Waffle House");
            }
            else if (MaxCount == WalmartCount && WalmartCount != 0)
            {
                return ("Walmart");
            }
            else if (MaxCount == StarbucksCount && StarbucksCount != 0)
            {
                return ("Starbucks");
            }
            else if (MaxCount == SamsClubCount && SamsClubCount != 0)
            {
                return ("Sam's Club");
            }
            else
            {
                return ("Unknown");
            }
        }
    }
}
