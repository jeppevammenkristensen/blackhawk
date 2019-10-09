using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blackhawk.Models.LanguageConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackhawk
{
    public class SourceBuilder
    {
        internal SourceStatus Status { get; private set; } = SourceStatus.Incomplete;
        private ILanguageConverter? _converter;

        public SourceBuilder WithConverter(ILanguageConverter languageConverter)
        {
            _converter = languageConverter ?? throw new ArgumentNullException(nameof(languageConverter));
            Status = SourceStatus.ReadyForParsing;
            return this;
        }

        public async Task<Source> GenerateSourceFromFile(string filePath)
        {
            string sourceText;
            try
            {
                sourceText = await File.ReadAllTextAsync(filePath);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"An exception occurred trying to open file {filePath}",exception);
            }

            return GenerateSourceFromString(sourceText);
        }

        public Source GenerateSourceFromString(string source)
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

                    var memberDeclarationSyntaxes = SyntaxFactory.ParseCompilationUnit(result.SourceConverter).Members;
                    if (!memberDeclarationSyntaxes.All(x => x is MethodDeclarationSyntax))
                    {
                        throw new InvalidOperationException($"The SourceConverter result consist only of one or more methods:{Environment.NewLine}{result.SourceConverter}");
                    }
                    
                    return new Source
                    {
                        ClassSources =
                            new ReadOnlyCollection<ClassFile>(classes.Select(x => new ClassFile(x)).ToList()),
                        PrimarySource = primary,
                        ParseMethod = new Method(memberDeclarationSyntaxes.Cast<MethodDeclarationSyntax>().ToArray()),
                        OriginalSource = source,
                        InputIsEnumerable = result.InputIsEnumerable
                    }.AddReferences(result.References.ToArray());
                }

                return Source.Invalid(details);
            }

            throw new InvalidOperationException(
                $"Trying to generate Source but no Converter has been defined. Call {nameof(WithConverter)} with a relevant converter");

        }
    }
}