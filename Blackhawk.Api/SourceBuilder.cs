using System;
using System.Collections.ObjectModel;
using System.Linq;
using Blackhawk.Models.LanguageConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackhawk
{
    public class SourceBuilder
    {
        internal SourceStatus Status { get; private set; } = SourceStatus.Incomplete;
        private ILanguageConverter? _converter = null;

        public SourceBuilder WithConverter(ILanguageConverter languageConverter)
        {
            _converter = languageConverter ?? throw new ArgumentNullException(nameof(languageConverter));
            Status = SourceStatus.ReadyForParsing;
            return this;
        }

        public Source GenerateSource(string source)
        {
            if (_converter != null)
            {
                var (success, details) = _converter.InputIsValid(source);
                if (success)
                {
                    var result = _converter.GenerateCsharp(source);
                    var compilationUnit = SyntaxFactory.ParseCompilationUnit(result.Classes);

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

                    if (!(SyntaxFactory.ParseMemberDeclaration(result.SourceConverter) is MethodDeclarationSyntax method
                        ))
                    {
                        throw new InvalidOperationException(
                            $"Source converter {this._converter.GetType().Name} generated invalid code for generating the method");
                    }

                    return new Source()
                    {
                        ClassSources =
                            new ReadOnlyCollection<ClassFile>(classes.Select(x => new ClassFile(x)).ToList()),
                        PrimarySource = primary,
                        ParseMethod = new Method(method),
                        OriginalSource = source,
                        InputIsEnumerable = result.InputIsEnumerable
                    };
                }
                else
                {
                    return Source.Invalid(details);
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Trying to generate Source but no Converter has been defined. Call {nameof(WithConverter)} with a relevant converter");
            }
       
        }
    }
}