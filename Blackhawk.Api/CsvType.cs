using System.Collections.Generic;

namespace Blackhawk
{
    public class CsvType
    {
        public CsvType()
        {
            Fields = new List<CsvField>();
        }
        public List<CsvField> Fields { get; set; }
    }
}