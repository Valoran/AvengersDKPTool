using System;
using System.Collections.Generic;
using System.Text;

namespace AvengersDKPTool.Models
{
    public class LootGridModel
    {
        public DateTime Date { get; set; }
        public string Charname { get; set; }
        public bool CharnameFound { get; set; }
        public string ItemName { get; set; }
        public  string Type { get; set; }
        public int Cost { get; set; }
        public decimal Calculated { get; set; }
        public bool Upload { get; set; }
    }
}
