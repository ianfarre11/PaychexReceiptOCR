using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PaychexReceiptOCR.Helpers
{
    public class ImageProcessingMethods
    {
        // Retrieves image file from the given path and fixes
        // potential orientation bug from images taken on a cellphone
        static public void ImageOrient(string path)
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
    }
}
