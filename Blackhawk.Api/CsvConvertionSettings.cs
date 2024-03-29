﻿namespace Blackhawk
{
    public class CsvConvertionSettings
    {
        public string Delimiter { get; set; } = ",";

        public bool FirstLineContainsHeaders { get; set; } = false;

        public static CsvConvertionSettings Create(string delimiter, bool firstLineContainsHeaders)
        {
            return new CsvConvertionSettings
            {
                Delimiter = delimiter,
                FirstLineContainsHeaders = firstLineContainsHeaders
            };
        }

    }
}