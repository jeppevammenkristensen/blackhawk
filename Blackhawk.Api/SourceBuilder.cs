using System;
using System.Collections.ObjectModel;
using System.Linq;
using Blackhawk.Models.LanguageConverter;
using Microsoft.CodeAnalysis.CSharp;

namespace Blackhawk
{
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

        public Source GenerateSource(string source)
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

                return new Source()
                {
                       ClassSources = new ReadOnlyCollection<ClassFile>(classes.Select(x => new ClassFile(x)).ToList()),
                    PrimarySource = primary
                };
            }
            else
            {
                return Source.Invalid(details);
            }

            //
            //var result = _converter.GenerateSource(source);
        }
    }
}