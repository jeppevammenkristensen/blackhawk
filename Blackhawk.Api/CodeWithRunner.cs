using System;
using System.Collections.Generic;
using System.Linq;
using Blackhawk.Models.LanguageConverter;
using Blackhawk.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Converters;

namespace Blackhawk
{
    public class Build
    {
        public static Source Source()
        {
            return new Source();
        } 
    }


    public class CodeExecutable
    {

    }

    public enum SourceStatus
    {
        Incomplete,
        ReadyForParsing
    }

    public class Source
    {
        internal List<ClassDeclarationSyntax> ClassSources { get; set; }
        internal ClassDeclarationSyntax PrimarySource { get; set; }

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
                var compilationUnit = SyntaxFactory.ParseCompilationUnit(source);
                var classDeclarationSyntax = compilationUnit.Members.OfType<ClassDeclarationSyntax>().ToList();
                var primary =
                    classDeclarationSyntax.FirstOrDefault(x => x.Identifier.ToString() == result.PrimaryClass);

                if (primary == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to find a primary class with the name {result.PrimaryClass} in the list of generated classed");
                }

                return new Source()
                {
                    ClassSources = classDeclarationSyntax,
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