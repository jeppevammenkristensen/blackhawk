using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Blackhawk.Models.Extensions;
using Blackhawk.Models.LanguageConverter;

namespace Blackhawk
{
    public class CsvLanguageConverter : ILanguageConverter
    {
        public CsvLanguageConverter(CsvConvertionSettings convertionSettings)
        {
            _settings = convertionSettings;
        }

        private static Regex GetParser(string delimiter)
        {
            return new Regex(GetSplitRegexString(delimiter));
        }

        private static string GetSplitRegexString(string delimiter)
        {
            return string.Format($"{delimiter}(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
        }

        private static readonly Regex Double = new Regex("^\\s*[-]?\\d+[.]\\d+\\s*$");
        private static readonly Regex DoubleComma = new Regex("^\\s*[-]?\\d+[,]\\d+\\s*$");

        private static readonly Regex Number = new Regex("^\\s*[-]?\\d+\\s*$");
        private readonly CsvConvertionSettings _settings;

        public (bool success, string details) InputIsValid(string source)
        {
            try
            {
                GenerateCsharp(source);
                return (true, default!);
            }
            catch (Exception ex)
            {
                return (false, ex.ToString());
            }
        }

        public Conversion GenerateCsharp(string input)
        {
            var items = GetLines(input, _settings).Take(51).ToArray();

            var header = _settings.FirstLineContainsHeaders ? items[0] : GenerateDummyHeaders(items[0].Length);
            var itemsToSkip = _settings.FirstLineContainsHeaders ? 1 : 0;
            var type = BuildCsvType(header, items.Skip(itemsToSkip).ToList());

            var cls = BuildClass(type);
            return new Conversion(PrimaryClass, cls, true, BuildSerializer(type))
                .WithReference(new Reference(typeof(Regex).Assembly,"System.Text.RegularExpressions"))
                .WithReference(new Reference(typeof(StringReader).Assembly,"System.IO"));
        }


        private string[] GenerateDummyHeaders(int length)
        {
            return Enumerable.Range(1, length).Select(x => $"Column{x}").ToArray();
        }

        private string BuildClass(CsvType type)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"public class {PrimaryClass}");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("");

            foreach (var field in type.Fields)
            {
                stringBuilder.AppendLine($"\t{field.GetPropertyDeclaration()}");
            }

            stringBuilder.AppendLine("}");

            var result = stringBuilder.ToString();
            return result;
        }

        public string BuildSerializer(CsvType type)
        {
            var stringBuilder = new StringBuilder();
            
            stringBuilder.AppendLine($@"public static List<{PrimaryClass}> Serialize(string source){{
    
    var result = new List<{PrimaryClass}>();
    Regex Parser = new Regex(""{_settings.Delimiter}(?=(?:[^\""]*\""[^\""]*\"")*(?![^\""]*\""))"");

    using (var reader = new StringReader(source)){{
        
        {(_settings.FirstLineContainsHeaders ? "reader.ReadLine();" : string.Empty)}
        
        while (reader.Peek() > -1){{
            var items = Parser.Split(reader.ReadLine());
            result.Add(CreateItem(items));
        }}
    }}

    return result;
}}");
            stringBuilder.AppendLine($@"public static {PrimaryClass} CreateItem(string[] row){{
    var result = new {PrimaryClass}();");

            foreach (var (field, index) in type.Fields.Select((x, i) => (x, i)))
            {
                stringBuilder.AppendLine(field.WriteParseSetter(index, "row"));
            }

            stringBuilder.AppendLine(@"return result;
}");

            return stringBuilder.ToString();
        }

        private IEnumerable<string[]> GetLines(string input, CsvConvertionSettings settings)
        {
            using (var reader = new StringReader(input))
            {
                while (reader.Peek() > -1)
                {
                    yield return GetParser(settings.Delimiter).Split(reader.ReadLine() ?? String.Empty);
                }
            }
        }

        private CsvType BuildCsvType(string[] columnNames, List<string[]> rows)
        {
            var result = new CsvType();
            var dictionary = new Dictionary<string, int>();

            var items = columnNames.Zip(columnNames
                .Select((_, x) => rows.Select(y => y[x])), (columnName, values) => (columnName, values));

            foreach ((string columnName, IEnumerable<string> values) column in items)
            {
                var type = new CsvField();
                var name = column.columnName.ToTitleCase();

                if (dictionary.ContainsKey(name))
                {
                    string safeName = name.SafePropertyName();
                    type.MemberName = safeName + dictionary[safeName];
                    dictionary[safeName] = dictionary[safeName]++;
                }
                else
                {
                    string safeName = name.SafePropertyName();
                    type.MemberName = safeName;
                    dictionary.Add(safeName, 1);
                }

                var (csvTypeEnum, nullable) = DetermineType(column.values);
                type.Type = csvTypeEnum;
                type.Nullable = nullable;
                result.Fields.Add(type);
            }

            return result;
        }

        private (CsvTypeEnum type, bool nullable) DetermineType(IEnumerable<string> column)
        {
            CsvTypeEnum? bestmatch = null;
            bool nullable = false;

            foreach (var item in column)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    nullable = true;
                    continue;
                }

                if (Double.IsMatch(item))
                {
                    bestmatch = CsvTypeEnum.Decimal;
                }
                else if (DoubleComma.IsMatch(item))
                {
                    bestmatch = CsvTypeEnum.DecimalComma;
                }

                else if (Number.IsMatch(item))
                {
                    bestmatch = CsvTypeEnum.Int;
                }
                else if (bool.TryParse(item, out bool _))
                {
                    bestmatch = CsvTypeEnum.Boolean;
                }
                else
                {
                    return (CsvTypeEnum.String, nullable);
                }
            }

            return (bestmatch ?? CsvTypeEnum.String, nullable);
        }


        public string PrimaryClass { get; set; } = "ReturnObject";

//        public object Deserialize(Type type, string requestSource)
//        {
//            return type.GetMethod("Serialize", BindingFlags.Public | BindingFlags.Static)
//                .Invoke(null, new object[] {requestSource});
//        }
    }
}