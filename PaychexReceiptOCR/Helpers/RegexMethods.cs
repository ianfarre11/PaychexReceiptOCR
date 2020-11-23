﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PaychexReceiptOCR.Models;

namespace PaychexReceiptOCR.Helpers
{
    public class RegexMethods
    {
        // Identifies if receipt is from Starbucks, Walmart, WaffleHouse, or other 
        static public string IdentifyVendor(string rawText, string contentRootPath)
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
            foreach (int i in CountList)
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

        public static void FindDateAndCost(Receipt receipt)
        {
            if (receipt.Vendor == "Walmart")
            {
                Regex rxTotalCost = new Regex(@"(?<=\bT\wT\wL\s+)\S+");
                receipt.TotalCost = rxTotalCost.Match(receipt.RawText).ToString();

                Regex rxDate = new Regex(@"\d+/\d+/\d+");
                receipt.Date = rxDate.Match(receipt.RawText).ToString();
            } else if (receipt.Vendor == "Starbucks")
            {
                Regex rxTotalCost = new Regex(@"(?<=\bTota\w\s+)\S+");
                receipt.TotalCost = rxTotalCost.Match(receipt.RawText).ToString();

                Regex rxDate = new Regex(@"\d+/\d+/\d+");
                receipt.Date = rxDate.Match(receipt.RawText).ToString();
            } else if (receipt.Vendor == "Waffle House")
            {
                Regex rxTotalCost = new Regex(@"(?<=\bT\wT\wL\s+)\S+");
                receipt.TotalCost = rxTotalCost.Match(receipt.RawText).ToString();

                Regex rxDate = new Regex(@"\d+/\d+/\d+");
                receipt.Date = rxDate.Match(receipt.RawText).ToString();
            } else if (receipt.Vendor == "Sam's Club")
            {
                Regex rxTotalCost = new Regex(@"(?<=\bT\wT\wL\s+)\S+");
                receipt.TotalCost = rxTotalCost.Match(receipt.RawText).ToString();

                Regex rxDate = new Regex(@"\d+/\d+/\d+");
                receipt.Date = rxDate.Match(receipt.RawText).ToString();
            }

            if (receipt.Date == "")
            {
                receipt.Date = "Unknown";
            }
            if (receipt.TotalCost == "")
            {
                receipt.TotalCost = "Unknown";
            }
        }
    }
}
