using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackhawk
{
    public class JsonFindMethod : CSharpSyntaxWalker
    {
        public MethodDeclarationSyntax? Method { get; private set; }

        public JsonFindMethod(string className, string methodName)
        {
            ClassName = className;
            MethodName = methodName;
        }

        public string ClassName { get; }
        public string MethodName { get; }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.Identifier.ToString() != ClassName)
                base.VisitClassDeclaration(node);
            else
            {
                var methodDeclarationSyntax = node.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(x => x.Identifier.ToString() == MethodName);
                if (methodDeclarationSyntax != null)
                    Method = methodDeclarationSyntax;
                else
                {
                    base.VisitClassDeclaration(node);
                }
            }

        }
    }
}