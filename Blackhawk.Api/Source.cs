using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Newtonsoft.Json;

namespace Blackhawk
{
    public class Source
    {
        internal const string ParserClassName = "ParsingUtility";
        internal const string AsyncRunMethodName = "RunAsync";
        internal const string RunnerClassName = "Runner";

        public ReadOnlyCollection<ClassFile> ClassSources { get; internal set; } = new ReadOnlyCollection<ClassFile>(new List<ClassFile>());
        public ClassDeclarationSyntax PrimarySource { get; internal set; } = default!;

        public List<Reference> References { get; } = new List<Reference>();

        internal Source()
        {
            References.Add(new Reference(typeof(JsonArrayAttribute).GetTypeInfo()
                .Assembly, "Newtonsoft.Json"));
            References.Add(new Reference(typeof(string).GetTypeInfo().Assembly, "System", "System.Threading.Tasks"));
            References.Add(new Reference(typeof(IEnumerable<>).GetTypeInfo().Assembly, "System.Collections.Generic"));
            References.Add(new Reference(typeof(Enumerable).GetTypeInfo().Assembly, "System.Linq"));
            References.Add(new Reference(Assembly.Load("netstandard, Version=2.0.0.0")));
            References.Add(new Reference(typeof(GeneratedCodeAttribute).GetTypeInfo()
                .Assembly));
            References.Add(new Reference(typeof(Attribute).GetTypeInfo()
                .Assembly));
            References.Add(new Reference(Assembly.Load("System.Runtime, Version=4.2.1.0")));
        }

        public Source AddReferences(params Reference[] reference)
        {
            References.AddRange(reference);
            return this;
            
        }

        public string InputParameterName { get; private set; } = "input";
        public Method ParseMethod { get; set; }
        public string OriginalSource { get; set; }
        public bool InputIsEnumerable { get; set; }

        public Source WithInputParameterName(string inputParameterName)
        {
            InputParameterName = inputParameterName ?? throw new ArgumentNullException(nameof(inputParameterName));
            return this;
        }

        public CompilationUnitSyntax BuildBaseCompilation()
        {
            var customWorkSpace = new AdhocWorkspace();

            var usingStatements = References.SelectMany(x => x._usings)
                .Select(x => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(x))).ToArray();

            var compilationUnitSyntax = SyntaxFactory.CompilationUnit()
                .AddUsings(usingStatements)
                .AddMembers(UtilityClass())
                .AddMembers(ClassSources
                    .Select(x => x.ClassDeclarationSyntax)
                    .OfType<MemberDeclarationSyntax>()
                    .ToArray()).NormalizeWhitespace();


            return (CompilationUnitSyntax)Formatter.Format(compilationUnitSyntax, customWorkSpace);

        }

        public MemberDeclarationSyntax[] UtilityClass()
        {

            var classDeclarationSyntax = SyntaxFactory.ClassDeclaration(ParserClassName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            
            foreach (var methodDeclarationSyntax in ParseMethod.DeclarationSyntaxes)
            {
                var publicStaticMethod = methodDeclarationSyntax
                    .WithModifiers(new SyntaxTokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)));
                classDeclarationSyntax = classDeclarationSyntax.AddMembers(publicStaticMethod);
            }

            return new MemberDeclarationSyntax[]
            {
                classDeclarationSyntax
            };
        }


        public CompilationUnitSyntax BuildCompilation(string source)
        {
            var blockSyntax = (BlockSyntax)SyntaxFactory.ParseStatement($"{{ {source} }}");

            var customWorkSpace = new AdhocWorkspace();

            var usingStatements = References.SelectMany(x => x._usings)
                .Select(x => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(x))).ToArray();

            var inputType = InputIsEnumerable
                ? SyntaxFactory.ParseTypeName($"List<{PrimarySource.Identifier}>")
                : SyntaxFactory.IdentifierName(PrimarySource.Identifier);

            var runnerMethod =
                SyntaxFactory.MethodDeclaration(
                        // returnType: Task<Object> // MethodName RunAsync  
                        returnType: SyntaxFactory.GenericName(SyntaxFactory.Identifier("Task"))
                            .AddTypeArgumentListArguments(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))), AsyncRunMethodName
                    )
                    // public static async
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                        SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                    // Input parameter for instance ReturnObject input
                    .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier(InputParameterName))
                        .WithType(inputType))
                    .AddBodyStatements(blockSyntax);

            var classDeclaration = SyntaxFactory.ClassDeclaration(RunnerClassName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddMembers(runnerMethod);

            var compilationUnitSyntax = SyntaxFactory.CompilationUnit()
                .AddUsings(usingStatements)
                .AddMembers(UtilityClass())
                .AddMembers(classDeclaration)
                .AddMembers()
                .AddMembers(ClassSources
                    .Select(x => x.ClassDeclarationSyntax)
                    .Cast<MemberDeclarationSyntax>()
                    .ToArray()).NormalizeWhitespace();

            return (CompilationUnitSyntax)Formatter.Format(compilationUnitSyntax, customWorkSpace);
        }


        public CompiledCode Compile(CompilationUnitSyntax syntax)
        {
            CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

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
}