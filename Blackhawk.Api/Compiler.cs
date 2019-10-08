using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackhawk
{
    public static class Compiler
    {
        public static CompiledCode Compile(Source source, CompilationUnitSyntax compilationUnit)
        {
            return source.Compile(compilationUnit);
        }
    }
}