using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackhawk.Parsing
{
    public class PropertyVisitor : CSharpSyntaxWalker
    {
        public List<PropertyInformation> Properties = new List<PropertyInformation>();

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Parent is ClassDeclarationSyntax c && c.Identifier.ToString() != "Runner")
            {
                Properties.Add(new PropertyInformation(){
                    Name = node.Identifier.ToString(),
                    Source = c.Identifier.ToString(),
                    TypeName = node.Type.ToString(),
                });
            }
        }
    }
}