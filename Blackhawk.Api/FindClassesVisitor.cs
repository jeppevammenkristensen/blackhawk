using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackhawk
{
    public class FindClassesVisitor : CSharpSyntaxWalker
    {
        public List<ClassDeclarationSyntax> Classes { get; } = new List<ClassDeclarationSyntax>();

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            Classes.Add(node);
            base.VisitClassDeclaration(node);
        }
    }
}