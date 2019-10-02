using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Blackhawk.Extensions
{

    public static class StringExtensions
    {
        public static string FormatCsharp(this string source)
        {
            return SyntaxFactory.ParseCompilationUnit(source).NormalizeWhitespace().ToString();
        }
    }

    public static class ReplExtensions
    {
        public static Repl Repl(this Source source)
        {
            var compilationUnitSyntax = source.BuildBaseCompilation();
            var compiledCode = source.Compile(compilationUnitSyntax);
            if (!compiledCode.Success)
            {
                throw new InvalidOperationException("Could not create Repl, source did not compile");
            }

            return new Repl(source)
            {

            };
        }
    }
}