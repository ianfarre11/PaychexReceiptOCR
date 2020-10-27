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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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

        private static Bitmap GetArgbCopy(Image sourceImage)
        {
            Bitmap bmpNew = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb);


            using (Graphics graphics = Graphics.FromImage(bmpNew))
            {
                graphics.DrawImage(sourceImage, new Rectangle(0, 0, bmpNew.Width, bmpNew.Height), new Rectangle(0, 0, bmpNew.Width, bmpNew.Height), GraphicsUnit.Pixel);
                graphics.Flush();
            }


            return bmpNew;
        }

        public static Bitmap CopyAsGrayscale(Image sourceImage)
        {
            Debug.WriteLine("This was called");
            Bitmap bmpNew = GetArgbCopy(sourceImage);
            BitmapData bmpData = bmpNew.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);


            IntPtr ptr = bmpData.Scan0;


            byte[] byteBuffer = new byte[bmpData.Stride * bmpNew.Height];


            Marshal.Copy(ptr, byteBuffer, 0, byteBuffer.Length);


            float rgb = 0;


            for (int k = 0; k < byteBuffer.Length; k += 4)
            {
                rgb = byteBuffer[k] * 0.11f;
                rgb += byteBuffer[k + 1] * 0.59f;
                rgb += byteBuffer[k + 2] * 0.3f;


                byteBuffer[k] = (byte)rgb;
                byteBuffer[k + 1] = byteBuffer[k];
                byteBuffer[k + 2] = byteBuffer[k];


                byteBuffer[k + 3] = 255;
            }


            Marshal.Copy(byteBuffer, 0, ptr, byteBuffer.Length);


            bmpNew.UnlockBits(bmpData);


            bmpData = null;
            byteBuffer = null;


            return bmpNew;
        }

        public Bitmap RemoveNoise(Bitmap bmap)
        {

            for (var x = 0; x < bmap.Width; x++)
            {
                for (var y = 0; y < bmap.Height; y++)
                {
                    var pixel = bmap.GetPixel(x, y);
                    if (pixel.R < 162 && pixel.G < 162 && pixel.B < 162)
                        bmap.SetPixel(x, y, Color.Black);
                    else if (pixel.R > 162 && pixel.G > 162 && pixel.B > 162)
                        bmap.SetPixel(x, y, Color.White);
                }
            }

            return bmap;
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

                            Bitmap img2 = new Bitmap(img1);

                            Bitmap img2Rgbd = GetArgbCopy(img2);

                            Bitmap img2Grey = CopyAsGrayscale(img2Rgbd);

                            img2Grey.SetResolution(200.0F, 200.0F);

                            //Bitmap imgNoNoise = RemoveNoise(img2Grey);

                            Image img1Better = FixedSize((Image)img2Grey, img2Grey.Width *2, img2Grey.Height*2);
                            Debug.WriteLine(img1Better.Width + " " + img1Better.Height);
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
            }

            // Passes List of receipts to Post view
            return View(model);
        }
    }
}
