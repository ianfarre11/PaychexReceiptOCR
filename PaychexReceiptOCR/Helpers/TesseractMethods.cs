using PaychexReceiptOCR.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Tesseract;

namespace PaychexReceiptOCR.Helpers
{
    public class TesseractMethods
    {
        static public Receipt OCRRead(Receipt receipt, string contentRootPath)
        {
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

        static public List<string> IteratePage(Page page)
        {
            // Holds the iterated text data 
            List<string> iterated = new List<string>();

            // Iterates through the tesseract page  
            using (var iter = page.GetIterator())
            {
                iter.Begin();

                do
                {
                    iterated.Add(iter.GetText(PageIteratorLevel.TextLine));
                } while (iter.Next(PageIteratorLevel.TextLine));
            }

            return iterated;
        }
    }
}
