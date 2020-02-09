using System;
using System.Collections.Generic;
using System.Text;

namespace AvengersDKPTool.Models
{
    public class LootGridModel
    {
        public string Date { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Item { get; set; }
        public  string Type { get; set; }
        public int Cost { get; set; }
        public decimal Calculated { get; set; }
    }
}
