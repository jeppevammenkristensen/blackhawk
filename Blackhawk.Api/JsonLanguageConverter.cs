using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Blackhawk.Models.LanguageConverter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using Conversion = Blackhawk.Models.LanguageConverter.Conversion;

namespace Blackhawk
{
    public class JsonConvertionSettings
    {
        public bool UsePascalCase { get; set; } = true;

        public JsonConvertionSettings WithPascalCase(bool usePascalCase)
        {
            UsePascalCase = usePascalCase;
            return this;
        }
    }

    public class JsonLanguageConverter : ILanguageConverter
    {
        private readonly JsonConvertionSettings _settings;

        public JsonLanguageConverter(JsonConvertionSettings settings)
        {
            _settings = settings;
        }

        public (bool success, string details) InputIsValid(string source)
        {
            try
            {
                JToken.Parse(source);
                return (true, null);
            }
            catch (JsonReaderException e)
            {
                return (false, e.Message);
            }
        }

        public Conversion GenerateCsharp(string input)
        {
            var csharpSettings = new CSharpGeneratorSettings();
            csharpSettings.ClassStyle = CSharpClassStyle.Poco;
            csharpSettings.TypeNameGenerator = new CustomDefaultTypeName(PrimaryClass);
            csharpSettings.GenerateJsonMethods = true;

            var schema = JsonSchema.FromSampleJson(input);

            var cSharpGenerator = new CSharpGenerator(schema,csharpSettings);
            var result = cSharpGenerator.GenerateFile();
            var classDeclarationSyntaxs = SyntaxFactory.ParseCompilationUnit(result).DescendantNodes().OfType<ClassDeclarationSyntax>().Cast<MemberDeclarationSyntax>().ToArray();

            string resultString = SyntaxFactory.CompilationUnit().AddMembers(classDeclarationSyntaxs).NormalizeWhitespace().ToString();

            return new Conversion(PrimaryClass,resultString, false);
        }

        public string PrimaryClass { get; set; } = "ReturnObject";

        public object Deserialize(Type parameterType, string requestSource)
        {
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(parameterType);

            var result = JToken.Parse(requestSource);
            if (result is JObject jObject)
            {
                var arr = new JArray { jObject };
                requestSource = arr.ToString();
            }

            return JsonConvert.DeserializeObject(
                requestSource, enumerableType,
                new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
    }

    public class CustomDefaultTypeName : DefaultTypeNameGenerator
    {
        private readonly string _defaultName;

        public CustomDefaultTypeName(string defaultName)
        {
            _defaultName = defaultName;
        }

        public override string Generate(JsonSchema schema, string typeNameHint, IEnumerable<string> reservedTypeNames)
        {
            if (string.IsNullOrWhiteSpace(typeNameHint))
            {
                return _defaultName;
            }
            return base.Generate(schema, typeNameHint, reservedTypeNames);
        }
    }


}
