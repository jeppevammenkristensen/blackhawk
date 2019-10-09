using System;
using System.Threading.Tasks;

namespace Blackhawk
{
    public class Repl
    {
        public Source Source { get; }

        internal Repl(Source source)
        {
            Source = source;
        }

        public async Task<(object? result, CompiledCode code)> Execute(string repl)
        {
            var compilationUnitSyntax = Source.BuildCompilation(repl);
            var compiledCode = Source.Compile(compilationUnitSyntax);
            if (compiledCode.Success)
            {
                var parserType = compiledCode.Assembly?.GetType(Source.ParserClassName);
                if (parserType == null)
                    throw new InvalidOperationException(
                        $"Failed to locate parser type {Source.ParserClassName}{Environment.NewLine}{compilationUnitSyntax}");

                var parseMethod = parserType.GetMethod(Source.ParseMethod.MethodName);
                if (parseMethod == null) throw new InvalidOperationException("Failed to locate method for parsing");

                var mainType = compiledCode.Assembly?.GetType(Source.RunnerClassName);
                if (mainType == null)
                {
                    throw new InvalidOperationException("Failed to load main type");
                }
                
                var methodInfo = mainType.GetMethod("RunAsync");
                if (methodInfo == null) throw new InvalidOperationException("Failed to locate method with name RunAsync");

                var task = (Task<object>)methodInfo.Invoke(null, new[] { parseMethod.Invoke(null, new object[] { Source.OriginalSource }) });

                var res = await task;
                return (res, compiledCode);
            }

            return (null, compiledCode);
        }
    }
}