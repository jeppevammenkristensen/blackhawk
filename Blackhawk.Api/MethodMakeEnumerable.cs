using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackhawk
{
    public class MethodMakeEnumerable : CSharpSyntaxRewriter
    {
        public MethodMakeEnumerable(string inputType)
        {
            InputType = inputType;
        }

        public string InputType { get; }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            node = node.WithReturnType(SyntaxFactory.ParseTypeName($"List<{InputType}>"));



            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            if (node.Identifier.ToString() != "List")
            {
                return base.VisitGenericName(node);
            }

            return node;
        }

        public override SyntaxNode VisitTypeArgumentList(TypeArgumentListSyntax node)
        {
            var first = node.Arguments.First();
            node = node.ReplaceNode(first, SyntaxFactory.ParseTypeName($"List<{InputType}>"));
            return base.VisitTypeArgumentList(node);
        }
    }
}