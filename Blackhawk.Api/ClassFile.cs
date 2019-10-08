using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackhawk
{
    public class ClassFile
    {
        internal ClassFile(ClassDeclarationSyntax classDeclarationSyntax)
        {
            SetValuesFromClassDeclaration(classDeclarationSyntax);

        }

        private void SetValuesFromClassDeclaration(ClassDeclarationSyntax classDeclarationSyntax)
        {
            ClassDeclarationSyntax = classDeclarationSyntax;
            Name = classDeclarationSyntax.Identifier.ToString();
            Text = classDeclarationSyntax.ToString();
        }

        public string Name { get; private set; } = default!;
        public string Text { get; private set; } = default!;

        public ClassDeclarationSyntax ClassDeclarationSyntax { get; private set; } = default!;

        public ClassFile UpdateText(string text)
        {
            var compilationUnit = SyntaxFactory.ParseCompilationUnit(text);
            if (compilationUnit.Members.Count > 1 ||
                !(compilationUnit.Members.FirstOrDefault() is ClassDeclarationSyntax))
            {
                throw new InvalidOperationException("The new value must be a single class");
            }


            SetValuesFromClassDeclaration((ClassDeclarationSyntax)compilationUnit.Members.First());

            return this;
        }

    }
}