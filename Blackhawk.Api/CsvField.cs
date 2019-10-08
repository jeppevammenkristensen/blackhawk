using System;

namespace Blackhawk
{
    public class CsvField
    {
        public string MemberName { get; set; } = default!;
        public CsvTypeEnum Type { get; set; }
        public bool Nullable { get; set; }

        public string GetPropertyDeclaration()
        {

            string typeName;

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

            return $"result.{MemberName} = {typeAsString}.Parse({sourceArrayName}[{index}]);";

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
}