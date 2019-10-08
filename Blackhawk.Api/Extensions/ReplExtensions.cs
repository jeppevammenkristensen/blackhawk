using System;
using System.Linq;

namespace Blackhawk.Extensions
{
    public static class ReplExtensions
    {
        public static Repl Repl(this Source source)
        {
            var compilationUnitSyntax = source.BuildBaseCompilation();
            var compiledCode = source.Compile(compilationUnitSyntax);
            if (!compiledCode.Success)
            {
                throw new InvalidOperationException($"Could not create Repl, source did not compile: Code: {Environment.NewLine} {compilationUnitSyntax.ToString()} {Environment.NewLine} {string.Join(Environment.NewLine,compiledCode.Diagnostics.Select(x => x.GetMessage()))}");
            }

            return new Repl(source);
        }
    }
}