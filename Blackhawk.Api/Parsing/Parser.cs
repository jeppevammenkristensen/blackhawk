using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;

namespace Blackhawk.Parsing
{
    public class Parser
    {
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(typeof(System.Uri).Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location);
        
        
        private static readonly MetadataReference JsonNetReference = MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonConvert).Assembly.Location);
        private static readonly MetadataReference NetStandard = MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location);

        private readonly string runner = @"
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.IO;
using System.Text.RegularExpressions;


public class Runner {{
    public object Run({1} input){{
        return input;
    }}
}}

 {0}
";

        public SyntaxNode GenerateSyntaxTree(string parsed, string inputType)
        {
            var root = SyntaxFactory.ParseCompilationUnit(string.Format(runner, parsed, inputType));
            return root.NormalizeWhitespace();  
        }


        public CompiledCode Compile(SyntaxNode syntax)
        {
            CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:OptimizationLevel.Debug);
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(string).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonPropertyAttribute).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IEnumerable<>).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(XmlRootAttribute).GetTypeInfo().Assembly.Location), 
                MetadataReference.CreateFromFile(typeof(Regex).GetTypeInfo().Assembly.Location), 
                MetadataReference.CreateFromFile(typeof(StringReader).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CultureInfo).GetTypeInfo().Assembly.Location),
                NetStandard,
                SystemRuntimeReference

            };

            var compilation = CSharpCompilation.Create("InMemory")
                .WithReferences(references)
                .WithOptions(options)
                .AddSyntaxTrees(syntax.SyntaxTree);
            return CompiledCode.Compile(compilation);
        }

    }
}