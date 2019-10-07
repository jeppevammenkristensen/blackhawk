using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Blackhawk.Models.LanguageConverter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using Newtonsoft.Json.Serialization;
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

            var (isArray, method) = DetermineEnumerabilityAndSourceMethod(input, classDeclarationSyntaxs);

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

    public class MethodMakeEnumerable : CSharpSyntaxRewriter
    {
        public MethodMakeEnumerable(string inputType)
        {
            InputType = inputType;
        }

        public string InputType { get; }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            node = node.WithReturnType(SyntaxFactory.ParseTypeName($"List<{InputType}>"));



            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            if (node.Identifier.ToString() != "List")
            {
                return base.VisitGenericName(node);
            }

            return node;
        }

        public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            var first = node.Arguments.First();
            node = node.ReplaceNode(first, SyntaxFactory.ParseTypeName($"List<{InputType}>"));
            return base.VisitTypeArgumentList(node);
        }
    }

    public class JsonFindMethod : CSharpSyntaxWalker
    {
        public MethodDeclarationSyntax? Method { get; private set; }

        public JsonFindMethod(string className, string methodName)
        {
            ClassName = className;
            MethodName = methodName;
        }

        public string ClassName { get; }
        public string MethodName { get; }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.Identifier.ToString() != ClassName)
                base.VisitClassDeclaration(node);
            else
            {
                var methodDeclarationSyntax = node.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(x => x.Identifier.ToString() == MethodName);
                if (methodDeclarationSyntax != null)
                    Method = methodDeclarationSyntax;
                else
                {
                    base.VisitClassDeclaration(node);
                }
            }

        }
    }


}
