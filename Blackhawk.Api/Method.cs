using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackhawk
{
    public class Method
    {
        internal Method(MethodDeclarationSyntax[] declarationSyntax)
        {
            if (declarationSyntax.Length < 1) throw new ArgumentException("Passed in parameter must have at least one item", nameof(declarationSyntax));
            
            DeclarationSyntaxes = declarationSyntax;
        }

        public MethodDeclarationSyntax[] DeclarationSyntaxes { get; private set; } 

        public string MethodName => DeclarationSyntaxes.First().Identifier.ToString();

        public override string ToString()
        {
            return string.Join(Environment.NewLine,DeclarationSyntaxes.Select(x => x.ToString()));
        }
    }
}