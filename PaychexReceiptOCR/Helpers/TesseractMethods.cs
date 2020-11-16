using PaychexReceiptOCR.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    }
}
