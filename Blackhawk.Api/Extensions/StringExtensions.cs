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
}