using Blackhawk.Models.LanguageConverter;

namespace Blackhawk
{
    public class CsvConvertionSettings : IConvertionSettings
    {
        public string Delimiter { get; set; }

        public bool FirstLineContainsHeaders { get; set; }

        public static CsvConvertionSettings Create(string delimiter, bool firstLineContainsHeaders)
        {
            return new CsvConvertionSettings()
            {
                Delimiter = delimiter,
                FirstLineContainsHeaders = firstLineContainsHeaders
            };
        }

    }
}