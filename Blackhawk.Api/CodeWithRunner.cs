using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blackhawk.Models.LanguageConverter;
using Blackhawk.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        public Reference(Assembly assembly,params string[] usings)
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
            References.Add(new Reference(typeof(string).GetTypeInfo().Assembly, "System"));
            References.Add(new Reference(typeof(IEnumerable<>).GetTypeInfo().Assembly, "System.Collections.Generic"));
            References.Add(new Reference(typeof(Enumerable).GetTypeInfo().Assembly,"System.Data.Linq"));
        }

        public string InputParameterName { get; private set; } = "input";

        public Source WithInputParameterName(string inputParameterName)
        {
            InputParameterName = inputParameterName ?? throw new ArgumentNullException(nameof(inputParameterName));
            return this;
        }

        public CompilationUnitSyntax BuildCompilation
        {
            get
            {
                var compilationUnitSyntax = CompilationUnit().AddUsings(References.SelectMany(x => x._usings)
                    .Select(x => UsingDirective(ParseName(x))).ToArray());

                compilationUnitSyntax = compilationUnitSyntax.AddMembers(
                    ClassDeclaration("Runner")
                        .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                        .AddMembers(ClassSources.ToArray())
                        .AddMembers(
                            MethodDeclaration(PredefinedType(Token(SyntaxKind.ObjectKeyword)), "RunAsync")
                                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword),
                                    Token(SyntaxKind.AsyncKeyword))
                                .AddParameterListParameters(Parameter(Identifier(InputParameterName))
                                    .WithType(IdentifierName(PrimarySource.Identifier))))).NormalizeWhitespace();

                return compilationUnitSyntax;

            }
        } 

        

        public static Source Invalid(string details)
        {
            return null;
        }
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