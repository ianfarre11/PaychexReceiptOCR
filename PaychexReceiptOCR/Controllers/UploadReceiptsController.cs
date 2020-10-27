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

        static Image FixedSize(Image imgPhoto, int Width, int Height)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)Width / (float)sourceWidth);
            nPercentH = ((float)Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = System.Convert.ToInt16((Width -
                              (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = System.Convert.ToInt16((Height -
                              (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(Width, Height,
                              PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                             imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.Red);
            grPhoto.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }

        public static bool IsPhoto(string fileName)
        {
            var list = ".jpg";
            var filename = fileName.ToLower();
            bool isThere = false;
              if (filename.EndsWith(list))
                {
                    isThere = true;
                }
            return isThere;
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
                        using (var img1 = new Bitmap(receipt.Path))
                        {
                            if(IsPhoto(receipt.Path) == true)
                            {
                                img1.RotateFlip(RotateFlipType.Rotate90FlipX);
                                img1.RotateFlip(RotateFlipType.RotateNoneFlipX);
                                Debug.WriteLine("The Image Flipped");
                            }
                           

                            Debug.WriteLine(img1.Width + " " + img1.Height);
                            
                            Image img1Better = FixedSize((Image)img1, img1.Width *2, img1.Height *2);
                            string wwwrootPath = _env.WebRootPath;
                            var ImagePath = @"userReceipts\";
                            var RelativeImagePath = ImagePath + "img1Better";
                            var AbsImagePath = Path.Combine(wwwrootPath, RelativeImagePath);

                            using (var fileStream = new FileStream(AbsImagePath, FileMode.Create))
                            {
                                img1Better.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                            }

                            // Loads receipt as Tesseract.Pix instance
                            using (var img = Pix.LoadFromFile(AbsImagePath))
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
                receipt.Vender = IdentifyVendor(receipt.RawText);

                // This section is incomplete but the idea is that a different series of regex operations will
                // occur depending on the Vender

                if (receipt.Vender == "Walmart")
                {
                    // Demo of Basic Regex Parsing
                    Regex rxTotalCost = new Regex(@"(?<=Total Price: )\S+");
                    receipt.TotalCost = rxTotalCost.Match(receipt.RawText).ToString();

                    Regex rxDate = new Regex(@"(?<=Date of Purchase: )\w+\s\d+\W\s\d+");
                    receipt.Date = rxDate.Match(receipt.RawText).ToString();

                    Regex rxTicketNumber = new Regex(@"(?<=Ti[c(]ket Number: )\d+");
                    receipt.TicketNumber = rxTicketNumber.Match(receipt.RawText).ToString();
                } else if (receipt.Vender == "Starbucks")
                {
                    // Demo of Basic Regex Parsing
                    Regex rxTotalCost = new Regex(@"(?<=Total Price: )\S+");
                    receipt.TotalCost = rxTotalCost.Match(receipt.RawText).ToString();

                    Regex rxDate = new Regex(@"(?<=Date of Purchase: )\w+\s\d+\W\s\d+");
                    receipt.Date = rxDate.Match(receipt.RawText).ToString();

                    Regex rxTicketNumber = new Regex(@"(?<=Ti[c(]ket Number: )\d+");
                    receipt.TicketNumber = rxTicketNumber.Match(receipt.RawText).ToString();
                } else if (receipt.Vender == "Waffle House")
                {
                    // Demo of Basic Regex Parsing
                    Regex rxTotalCost = new Regex(@"(?<=Total Price: )\S+");
                    receipt.TotalCost = rxTotalCost.Match(receipt.RawText).ToString();

                    Regex rxDate = new Regex(@"(?<=Date of Purchase: )\w+\s\d+\W\s\d+");
                    receipt.Date = rxDate.Match(receipt.RawText).ToString();

                    Regex rxTicketNumber = new Regex(@"(?<=Ti[c(]ket Number: )\d+");
                    receipt.TicketNumber = rxTicketNumber.Match(receipt.RawText).ToString();
                } else
                {
                    // Demo of Basic Regex Parsing
                    Regex rxTotalCost = new Regex(@"(?<=Total Price: )\S+");
                    receipt.TotalCost = rxTotalCost.Match(receipt.RawText).ToString();

                    Regex rxDate = new Regex(@"(?<=Date of Purchase: )\w+\s\d+\W\s\d+");
                    receipt.Date = rxDate.Match(receipt.RawText).ToString();

                    Regex rxTicketNumber = new Regex(@"(?<=Ti[c(]ket Number: )\d+");
                    receipt.TicketNumber = rxTicketNumber.Match(receipt.RawText).ToString();
                }
            }



            // Passes List of receipts to Post view
            return View(model);
        }
        // Identifys if receipt is from Starbucks, Walmart, WaffleHouse, or other 
        static string IdentifyVendor(string rawText)
        {
            int WaffleHouseCount = 0;
            int WalmartCount = 0;
            int StarbucksCount = 0;

            // Possible matches for WaffleHouse
            Regex rgx = new Regex(@"Entry Mode[!l]");
            if (rgx.IsMatch(rawText))
            {
                WaffleHouseCount += 2;
            }

            rgx = new Regex(@"Batch [8#][!:]");
            if (rgx.IsMatch(rawText))
            {
                WaffleHouseCount += 2;
            }

            rgx = new Regex(@"FFLE HOUSE");
            if (rgx.IsMatch(rawText))
            {
                WaffleHouseCount += 3;
            }

            rgx = new Regex(@"PRE-TIP \w\wt");
            if (rgx.IsMatch(rawText))
            {
                WaffleHouseCount += 2;
            }

            // Possible matches for Walmart
            rgx = new Regex(@"ITEMS SOLD");
            if (rgx.IsMatch(rawText))
            {
                WalmartCount++;
            }

            rgx = new Regex(@"\*\*\*Cust\w\w\w\w \w\wpy\*\*\*");
            if (rgx.IsMatch(rawText))
            {
                WalmartCount++;
            }

            rgx = new Regex(@"Save money");
            if (rgx.IsMatch(rawText))
            {
                WalmartCount += 2;
            }

            rgx = new Regex(@"Live better");
            if (rgx.IsMatch(rawText))
            {
                WalmartCount += 2;
            }

            // Possible matches for Starbucks
            rgx = new Regex(@"In\woice");
            if (rgx.IsMatch(rawText))
            {
                StarbucksCount++;
            }

            rgx = new Regex(@"CREDIT C\w\wD");
            if (rgx.IsMatch(rawText))
            {
                StarbucksCount++;
            }

            rgx = new Regex(@"TOT\wL");
            if (rgx.IsMatch(rawText))
            {
                StarbucksCount++;
            }

            rgx = new Regex(@"PRE-TIP");
            if (rgx.IsMatch(rawText))
            {
                StarbucksCount++;
            }

            int maxcount = Math.Max(Math.Max(WaffleHouseCount, WalmartCount), StarbucksCount);
            if (maxcount == WaffleHouseCount)
            {
                return ("Waffle House");
            }
            else if (maxcount == WalmartCount)
            {
                return ("Walmart");
            }
            else if (maxcount == StarbucksCount)
            {
                return ("Starbucks");
            }
            else
            {
                return ("Error");
            }
        }
    }
}
