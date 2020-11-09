using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaychexReceiptOCR.Models
{
    public class Receipt
    {
        public string Name { get; set; }

        public float MeanConfidence { get; set; }

        public string RawText { get; set; }

        public string Path { get; set; }

        public List<string> IteratedText { get; set; }

        public string TotalCost { get; set; }

        public string Date { get; set; }

        public string TicketNumber { get; set; }

        public string CardNumber { get; set; }

        public string Vendor { get; set; }
    }
}
