using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackhawk.Parsing
{
    /// <summary>
    /// Find the method Run and add the statement
    /// </summary>
    public class StatementRewriter : CSharpSyntaxRewriter
    {
        public StatementRewriter(string statement)
        {
            Statement = statement;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Identifier.ToString() != "Run")
            {
                return base.VisitMethodDeclaration(node);
            }
            
            var statement = (BlockSyntax)SyntaxFactory.ParseStatement($"{{{Statement}}}");
            return node.WithBody(statement).NormalizeWhitespace();


        }

        public string Statement { get; }
    }
}