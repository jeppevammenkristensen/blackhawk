using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private CsvConvertionSettings _settings;

        public (bool success, string details) InputIsValid(string source)
        {
            try
            {
                GenerateCsharp(source);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.ToString());
            }
        }

        public SourceLanguage Source => SourceLanguage.CSV;
                public Conversion GenerateCsharp(string input)
        {
            var items = GetLines(input, _settings).Take(51).ToArray();

            var header = _settings.FirstLineContainsHeaders ? items[0] : GenerateDummyHeaders(items[0].Length);
            var itemsToSkip = _settings.FirstLineContainsHeaders ? 1 : 0;
            var  type = BuildCsvType(header, items.Skip(itemsToSkip).ToList());

            var cls = BuildClass(type, _settings);
            return new Conversion(cls, "ReturnObject", true, string.Empty);
        }


        private string[] GenerateDummyHeaders(int length)
        {
            return Enumerable.Range(1, length).Select(x => $"Column{x}").ToArray();
        }

        private string BuildClass(CsvType type, CsvConvertionSettings settings)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("public class ReturnObject");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("");

            foreach (var field in type.Fields)
            {
                stringBuilder.AppendLine($"\t{field.GetPropertyDeclaration()}");
            }

            stringBuilder.AppendLine("");
            stringBuilder.AppendLine($@"public static ReturnObject[] Serialize(string source){{
    
    var result = new List<ReturnObject>();
    Regex Parser = new Regex(""{settings.Delimiter}(?=(?:[^\""]*\""[^\""]*\"")*(?![^\""]*\""))"");

    using (var reader = new StringReader(source)){{
        
        {(settings.FirstLineContainsHeaders ? "reader.ReadLine();" : string.Empty)}
        
        while (reader.Peek() > -1){{
            var items = Parser.Split(reader.ReadLine());
            result.Add(CreateItem(items));
        }}
    }}

    return result.ToArray();
}}");
            stringBuilder.AppendLine(@"public static ReturnObject CreateItem(string[] row){
    var result = new ReturnObject();");

            foreach (var (field, index) in type.Fields.Select((x,i) => (x,i)))
            {
                stringBuilder.AppendLine(field.WriteParseSetter(index, "row"));
         
            }

    stringBuilder.AppendLine(@"return result;
}");

            stringBuilder.AppendLine("}");



            var result = stringBuilder.ToString();
            return result;

        }

        public IEnumerable<string[]> GetLines(string input, CsvConvertionSettings settings)
        {
            using (var reader = new StringReader(input))
            {
                while (reader.Peek() > -1)
                {
                    yield return GetParser(settings.Delimiter).Split(reader.ReadLine() ?? String.Empty);
                }
            }
        }

        public CsvType BuildCsvType(string[] columnNames, List<string[]> rows)
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
                    return (CsvTypeEnum.String,nullable);
                }
            }

            return (bestmatch ?? CsvTypeEnum.String, nullable);

        }

        

        public string PrimaryClass { get; }
        public object Deserialize(Type type, string requestSource)
        {
            return type.GetMethod("Serialize", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] {requestSource});
        }
    }

    public class CsvField
    {
        public string MemberName { get; set; }
        public CsvTypeEnum Type { get; set; }
        public bool Nullable { get; set; }

        public string GetPropertyDeclaration()
        {

            string typeName = null;

            switch (Type)
            {
                case CsvTypeEnum.String:
                    typeName = "string";
                    break;
                case CsvTypeEnum.Decimal:
                case CsvTypeEnum.DecimalComma:
                    typeName = AppendNullableIfTrue("decimal");
                    break;
                case CsvTypeEnum.Int:
                    typeName = AppendNullableIfTrue("int");
                    break;
                case CsvTypeEnum.Boolean:
                    typeName = AppendNullableIfTrue("bool");
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"No support for {Type}");
                    
            }

            return $"public {typeName} {MemberName} {{get;set;}}";
        }

        private string AppendNullableIfTrue(string typeAsString)
        {
            if (!Nullable)
                return typeAsString;
            return $"{typeAsString}?";
        }       

        private string WriteParseMethod(string typeAsString, string sourceArrayName, int index)
        {
            if (Nullable)
            {
                return $@"if ({typeAsString}.TryParse({sourceArrayName}[{index}], out {typeAsString} v{index}))
{{
    result.{MemberName} = v{index};
}}";
            }
            else
            {
                return $"result.{MemberName} = {typeAsString}.Parse({sourceArrayName}[{index}]);";
            }
            
        }

        public string WriteParseSetter(int index, string sourceArrayName)
        {
            switch (Type)
            {
                case CsvTypeEnum.String:
                    return $"result.{MemberName} = {sourceArrayName}[{index}].Trim(' ').Trim('\"');";
                case CsvTypeEnum.Decimal:
                    return WriteParseMethod("decimal", sourceArrayName, index);
                case CsvTypeEnum.DecimalComma:
                    if (Nullable)
                    {
                        return $@"if (decimal.TryParse({sourceArrayName}[{index}],NumberStyles.Any,new CultureInfo(""da-DK""),out decimal v{index}))
{{                            
    result.{MemberName} = v{index};
}}";
                    }
                    else
                    {
                        return $"result.{MemberName} = decimal.Parse({sourceArrayName}[{index}],new CultureInfo(\"da-DK\"));";
                    }
                    
                case CsvTypeEnum.Int:
                    return WriteParseMethod("int", sourceArrayName, index);
                case CsvTypeEnum.Boolean:
                    return WriteParseMethod("bool", sourceArrayName, index);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class CsvType
    {
        public CsvType()
        {
            Fields = new List<CsvField>();
        }
        public List<CsvField> Fields { get; set; }
    }

    public enum CsvTypeEnum
    {
        String,
        Decimal,
        DecimalComma,
        Int,
        Boolean
    }
}