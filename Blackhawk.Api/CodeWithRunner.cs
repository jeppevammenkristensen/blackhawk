using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Blackhawk;
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
        internal const string ParserClassName = "ParsingUtility";
        internal const string AsyncRunMethodName = "RunAsync";
        internal const string RunnerClassName = "Runner";

        public ReadOnlyCollection<ClassFile> ClassSources { get; internal set; }
        public ClassDeclarationSyntax PrimarySource { get; internal set; }

        public List<Reference> References { get; } = new List<Reference>();

        internal Source()
        {
            References.Add(new Reference(typeof(JsonArrayAttribute).GetTypeInfo()
                .Assembly, "Newtonsoft.Json"));
            References.Add(new Reference(typeof(string).GetTypeInfo().Assembly, "System", "System.Threading.Tasks"));
            References.Add(new Reference(typeof(IEnumerable<>).GetTypeInfo().Assembly, "System.Collections.Generic"));
            References.Add(new Reference(typeof(Enumerable).GetTypeInfo().Assembly, "System.Linq"));
            References.Add(new Reference(Assembly.Load("netstandard, Version=2.0.0.0")));
            References.Add(new Reference(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).GetTypeInfo()
                .Assembly));
            References.Add(new Reference(typeof(Attribute).GetTypeInfo()
                .Assembly));
            References.Add(new Reference(Assembly.Load("System.Runtime, Version=4.2.1.0")));
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
                .Select(x => UsingDirective(ParseName(x))).ToArray();

            var compilationUnitSyntax = CompilationUnit()
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
            var publicStaticMethod = this.ParseMethod.DeclarationSyntax
                .WithModifiers(new SyntaxTokenList(Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword)));

            var classDeclarationSyntax = ClassDeclaration(ParserClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddMembers(publicStaticMethod);

            return new MemberDeclarationSyntax[]
            {
                classDeclarationSyntax
            };
        }


        public CompilationUnitSyntax BuildCompilation(string source)
        {
            var blockSyntax = (BlockSyntax)ParseStatement($"{{ {source} }}");

            var customWorkSpace = new AdhocWorkspace();

            var usingStatements = References.SelectMany(x => x._usings)
                .Select(x => UsingDirective(ParseName(x))).ToArray();

            var inputType = InputIsEnumerable
                ? ParseTypeName($"List<{PrimarySource.Identifier}>")
                : IdentifierName(PrimarySource.Identifier);

            var runnerMethod =
                MethodDeclaration(
                        // returnType: Task<Object> // MethodName RunAsync  
                        returnType: GenericName(Identifier("Task"))
                            .AddTypeArgumentListArguments(PredefinedType(Token(SyntaxKind.ObjectKeyword))), AsyncRunMethodName
                    )
                    // public static async
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.AsyncKeyword))
                    // Input parameter for instance ReturnObject input
                    .AddParameterListParameters(Parameter(Identifier(InputParameterName))
                        .WithType(inputType))
                    .AddBodyStatements(blockSyntax);

            var classDeclaration = ClassDeclaration(RunnerClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddMembers(runnerMethod);

            var compilationUnitSyntax = CompilationUnit()
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


    public class FindClassesVisitor : CSharpSyntaxWalker
    {
        public List<ClassDeclarationSyntax> Classes { get; } = new List<ClassDeclarationSyntax>();

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            Classes.Add(node);
            base.VisitClassDeclaration(node);
        }
    }

    public class Method
    {
        internal Method(MethodDeclarationSyntax declarationSyntax)
        {
            DeclarationSyntax = declarationSyntax;
        }

        public MethodDeclarationSyntax DeclarationSyntax { get; private set; }

        public string MethodName => DeclarationSyntax.Identifier.ToString();

        public string ToString()
        {
            return DeclarationSyntax.ToString();
        }
    }

    public class ClassFile
    {
        internal ClassFile()
        {

        }

        internal ClassFile(ClassDeclarationSyntax classDeclarationSyntax)
        {
            SetValuesFromClassDeclaration(classDeclarationSyntax);

        }

        private void SetValuesFromClassDeclaration(ClassDeclarationSyntax classDeclarationSyntax)
        {
            ClassDeclarationSyntax = classDeclarationSyntax;
            Name = classDeclarationSyntax.Identifier.ToString();
            Text = classDeclarationSyntax.ToString();
        }

        public string Name { get; private set; }
        public string Text { get; private set; }

        public ClassDeclarationSyntax ClassDeclarationSyntax { get; set; }

        public ClassFile UpdateText(string text)
        {
            var compilationUnit = SyntaxFactory.ParseCompilationUnit(text);
            if (compilationUnit.Members.Count > 1 ||
                !(compilationUnit.Members.FirstOrDefault() is ClassDeclarationSyntax))
            {
                throw new InvalidOperationException("The new value must be a single class");
            }


            SetValuesFromClassDeclaration((ClassDeclarationSyntax)compilationUnit.Members.First());

            return this;
        }

    }

    public class BlackhawkDownException : Exception
    {
        public string Message { get; }
        public string Description { get; }

        public BlackhawkDownException(string message, string description)
        {
            Message = message;
            Description = description;
        }
    }

    public class Repl
    {
        public Source Source { get; }

        internal Repl(Source source)
        {
            Source = source;
        }

        public async Task<(object? result, CompiledCode? code)> Execute(string repl)
        {
            var compilationUnitSyntax = Source.BuildCompilation(repl);
            var compiledCode = Source.Compile(compilationUnitSyntax);
            if (compiledCode.Success)
            {
                var parserType = compiledCode.Assembly.GetType(Source.ParserClassName);
                if (parserType == null)
                    throw new InvalidOperationException(
                        $"Failed to locate parser type {Source.ParserClassName}{Environment.NewLine}{compilationUnitSyntax}");

                var parseMethod = parserType.GetMethod(Source.ParseMethod.MethodName);
                if (parseMethod == null) throw new InvalidOperationException("Failed to locate method for parsing");

                var mainType = compiledCode.Assembly.GetType(Source.RunnerClassName);
                var methodInfo = mainType.GetMethod("RunAsync");
                if (methodInfo == null) throw new InvalidOperationException("Failed to locate method with name RunAsync");

                var task = (Task<object>)methodInfo.Invoke(null, new object[] { parseMethod.Invoke(null, new Object[] { Source.OriginalSource }) });

                var res = await task;
                return (res, compiledCode);
            }
            else
            {
                return (null, compiledCode);
            }
        }
    }


    public static class Compiler
    {
        public static CompiledCode Compile(Source source, CompilationUnitSyntax compilationUnit)
        {
            return source.Compile(compilationUnit);
        }
    }
}