using System;
using System.Collections.Generic;
using System.Linq;
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
                return (true, default!);
            }
            catch (JsonReaderException e)
            {
                return (false, e.Message);
            }
        }

        public Conversion GenerateCsharp(string input)
        {
            var csharpSettings = new CSharpGeneratorSettings
            {
                ClassStyle = CSharpClassStyle.Poco,
                TypeNameGenerator = new CustomDefaultTypeName(PrimaryClass),
                GenerateJsonMethods = true,
                
            };

            var schema = JsonSchema.FromSampleJson(input);

            var cSharpGenerator = new CSharpGenerator(schema,csharpSettings);
            var result = cSharpGenerator.GenerateFile();

            // Get all classes
            var classDeclarationSyntaxes = SyntaxFactory
                .ParseCompilationUnit(result)
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Cast<MemberDeclarationSyntax>()
                .ToArray();

            var resultString = SyntaxFactory
                .CompilationUnit()
                .AddMembers(classDeclarationSyntaxes)
                .NormalizeWhitespace()
                .ToString();

            var (isArray, method) = DetermineEnumerabilityAndSourceMethod(input, classDeclarationSyntaxes);

            return new Conversion(PrimaryClass,resultString, isArray,  method);
        }

        private (bool isArray, string method ) DetermineEnumerabilityAndSourceMethod(string input,
            MemberDeclarationSyntax[] classDeclarationSyntaxs)
        {
            var jToken = JToken.Parse(input);
            bool isArray = jToken is JArray;

            foreach (var memberDeclarationSyntax in classDeclarationSyntaxs)
            {
                var visitor = new JsonFindMethod(PrimaryClass, "FromJson");
                visitor.Visit(memberDeclarationSyntax);
                if (visitor.Method != null)
                {
                    var method = visitor.Method;

                    if (isArray)
                    {
                        var methodChanger = new MethodMakeEnumerable(PrimaryClass);
                        method = (MethodDeclarationSyntax) methodChanger.Visit(method);
                    }

                    return (isArray, method.ToString());
                }
            }

            throw new InvalidOperationException("Was not able to locate a method for parsing from string source");
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
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
    }
}
