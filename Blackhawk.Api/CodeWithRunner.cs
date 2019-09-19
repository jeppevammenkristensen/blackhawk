using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Blackhawk.Models.LanguageConverter;
using Blackhawk.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Newtonsoft.Json;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Blackhawk
{
    public class Build
    {
        public static SourceBuilder Init()
        {
            return new SourceBuilder();
        }
    }

    public enum SourceStatus
    {
        Incomplete,
        ReadyForParsing
    }

    public class Reference
    {
        public readonly Assembly _assembly;
        public readonly string[] _usings;

        public Reference(Assembly assembly, params string[] usings)
        {
            _assembly = assembly;
            _usings = usings;
        }
    }

    public class Source
    {
        public List<ClassDeclarationSyntax> ClassSources { get; set; }
        public ClassDeclarationSyntax PrimarySource { get; set; }

        public List<Reference> References { get; } = new List<Reference>();

        internal Source()
        {
            //MetadataReference.CreateFromFile(typeof(string).GetTypeInfo().Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(JsonPropertyAttribute).GetTypeInfo().Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(IEnumerable<>).GetTypeInfo().Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(XmlRootAttribute).GetTypeInfo().Assembly.Location), 
            //MetadataReference.CreateFromFile(typeof(Regex).GetTypeInfo().Assembly.Location), 
            //MetadataReference.CreateFromFile(typeof(StringReader).GetTypeInfo().Assembly.Location),
            //MetadataReference.CreateFromFile(typeof(CultureInfo).GetTypeInfo().Assembly.Location),
            References.Add(new Reference(typeof(JsonArrayAttribute).GetTypeInfo()
                .Assembly, "Newtonsoft.Json"));

            References.Add(new Reference(typeof(string).GetTypeInfo().Assembly, "System","System.Threading.Tasks"));
            References.Add(new Reference(typeof(IEnumerable<>).GetTypeInfo().Assembly, "System.Collections.Generic"));
            References.Add(new Reference(typeof(Enumerable).GetTypeInfo().Assembly, "System.Linq"));
            References.Add(new Reference(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).GetTypeInfo()
                .Assembly));
        }

        public string InputParameterName { get; private set; } = "input";

        public Source WithInputParameterName(string inputParameterName)
        {
            InputParameterName = inputParameterName ?? throw new ArgumentNullException(nameof(inputParameterName));
            return this;
        }

       
        public CompilationUnitSyntax BuildCompilation(string source)
        {
            var blockSyntax = (BlockSyntax)ParseStatement($"{{ {source} }}");

            var customWorkSpace = new AdhocWorkspace();
            var compilationUnitSyntax = CompilationUnit().AddUsings().NormalizeWhitespace();

            var usingStatements = References.SelectMany(x => x._usings)
                .Select(x => UsingDirective(ParseName(x))).ToArray();

            var runnerMethod =
                MethodDeclaration(
                        // returnType: Task<Object> // MethodName RunAsync  
                        returnType: GenericName(Identifier("Task"))
                            .AddTypeArgumentListArguments(PredefinedType(Token(SyntaxKind.ObjectKeyword))), "RunAsync"
                    )
                    // public static async
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.AsyncKeyword))
                    // Input parameter for instance ReturnObject input
                    .AddParameterListParameters(Parameter(Identifier(InputParameterName))
                        .WithType(IdentifierName(PrimarySource.Identifier)))
                    .AddBodyStatements(blockSyntax);

            var classDeclaration = ClassDeclaration("Runner")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddMembers(runnerMethod);


            compilationUnitSyntax = CompilationUnit()
                .AddUsings(usingStatements)
                .AddMembers(classDeclaration)
                .AddMembers(ClassSources.ToArray()).NormalizeWhitespace();


            return (CompilationUnitSyntax)Formatter.Format(compilationUnitSyntax, customWorkSpace);
        }

        public CompiledCode Compile(CompilationUnitSyntax syntax)
        {
            CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug);

            var compilation = CSharpCompilation.Create("InMemory")
                .WithReferences(References.Select(x => MetadataReference.CreateFromFile(x._assembly.Location)))
                .WithOptions(options)
                .AddSyntaxTrees(syntax.SyntaxTree);
            return CompiledCode.Compile(compilation);
        }

        public static Source Invalid(string details)
        {
            return null;
        }

        //public Task<object> ExecuteAsync(string returnInput)
        //{
        //    var buildCompilation = BuildCompilation(returnInput);
        //}
    }

    public class SourceBuilder
    {
        internal SourceStatus Status { get; private set; } = SourceStatus.Incomplete;
        private ILanguageConverter _converter = null;

        public SourceBuilder WithConverter(ILanguageConverter languageConverter)
        {
            _converter = languageConverter ?? throw new ArgumentNullException(nameof(languageConverter));
            Status = SourceStatus.ReadyForParsing;
            return this;
        }

        public Source GenerateCsharp(string source)
        {
            var (success, details) = _converter.InputIsValid(source);
            if (success)
            {
                var result = _converter.GenerateCsharp(source);
                var compilationUnit = ParseCompilationUnit(result.Classes);

                var finder = new FindClassesVisitor();
                finder.Visit(compilationUnit);
                var classes = finder.Classes;

                var primary =
                    classes.FirstOrDefault(x => x.Identifier.ToString() == result.PrimaryClass);

                if (primary == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to find a primary class with the name {result.PrimaryClass} in the list of generated classed");
                }

                return new Source()
                {
                    ClassSources = classes,
                    PrimarySource = primary
                };
            }
            else
            {
                return Source.Invalid(details);
            }

            //
            //var result = _converter.GenerateCsharp(source);
        }
    }




    public class FindClassesVisitor : CSharpSyntaxWalker
    {
        public List<ClassDeclarationSyntax> Classes { get; } = new List<ClassDeclarationSyntax>();

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            Classes.Add(node);
            base.VisitClassDeclaration(node);
        }
    }

    public class CSharpFile
    {
        public string Name { get; set; }
        public string Text { get; set; }

        public ClassDeclarationSyntax ClassDeclarationSyntax { get; set; }
    }

    /// <summary>
    /// The base code with the generated classes and the Runner class that is used to
    /// run the code
    /// </summary>
    public class CodeWithRunner
    {
        public CodeWithRunner(SyntaxNode basecode)
        {
            Basecode = basecode;
            var walker = new PropertyVisitor();
            walker.Visit(basecode);
            this.Properties = walker.Properties;
        }

        public List<PropertyInformation> Properties { get; set; }

        public SyntaxNode Basecode { get; set; }


        public SyntaxNode AddCode(string code)
        {
            var methodDeclaration = new StatementRewriter
            {
                Statement = code
            };
            return methodDeclaration.Visit(this.Basecode);
        }
    }
}