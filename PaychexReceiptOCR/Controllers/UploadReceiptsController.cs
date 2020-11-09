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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ImageMagick;

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

                    // Get the extension of the uploaded file.
                    string extension = System.IO.Path.GetExtension(AbsImagePath);

                    //Fixes rotation issues 
                    ImageOrient(newReceipt.Path);
                    receipts.Add(newReceipt);
                    
                }
            }

            // Gives the receipts list to the OCRRead method
            return OCRRead(receipts);
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

                // Identifys the Vender
                receipt.Vender = IdentifyVendor(receipt.RawText, contentRootPath);
            }
            // Passes List of receipts to Post view
            return View(model);
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
            RegexList = System.IO.File.ReadAllLines(Path.Combine(contentRootPath + "\\Properties\\Regex\\WalmartRegex.txt"));
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
            RegexList = System.IO.File.ReadAllLines(Path.Combine(contentRootPath + "\\Properties\\Regex\\WaffleHouseRegex.txt"));
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
            RegexList = System.IO.File.ReadAllLines(Path.Combine(contentRootPath + "\\Properties\\Regex\\StarbucksRegex.txt"));
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
            RegexList = System.IO.File.ReadAllLines(Path.Combine(contentRootPath + "\\Properties\\Regex\\SamsClubRegex.txt"));
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
