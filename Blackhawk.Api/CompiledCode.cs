using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Blackhawk
{
    public class CompiledCode   
    {
        public Assembly? Assembly { get; }

        private CompiledCode(EmitResult result, Assembly? assembly)
        {
            Success = result.Success;
            Diagnostics = result.Diagnostics.ToArray();    
            Assembly = assembly;

        }

        public Diagnostic[] Diagnostics { get; private set; } 

        public bool Success { get;  }

        public static CompiledCode Compile(CSharpCompilation compilation)
        {
            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                Assembly? assembly = null;
                if (result.Success)
                {
                    assembly = Assembly.Load(stream.ToArray());
                }

                return new CompiledCode(result,assembly);
            }
        }
    }
}