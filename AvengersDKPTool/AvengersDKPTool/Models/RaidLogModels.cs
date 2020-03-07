using System;
using System.Collections.Generic;
using System.Text;

namespace AvengersDKPTool.Models
{
   public class RaidLogFileModel
    {
        public DateTime Date { get; set; }
        public bool Parsed { get; set; }
        public string File { get; set; }
    }
}
