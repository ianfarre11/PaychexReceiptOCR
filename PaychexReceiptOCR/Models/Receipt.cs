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

        public List<string> IteratedText { get; set; }
    }
}
