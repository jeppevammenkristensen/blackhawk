using System;
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
        public Assembly Assembly { get; }

        private CompiledCode(EmitResult result, Assembly assembly)
        {
            Success = result.Success;
            Diagnostics = result.Diagnostics.ToArray();    
            Assembly = assembly;

        }

        public Diagnostic[] Diagnostics { get; set; }

        public bool Success { get;  }

        public static CompiledCode Compile(CSharpCompilation compilation)
        {
            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                Assembly assembly = null;
                if (result.Success)
                {
                    assembly = System.Reflection.Assembly.Load(stream.ToArray());
                }

                return new CompiledCode(result,assembly);
            }
        }

        public Type GetMainType(string mainType)
        {
            return Assembly.GetType(mainType);
        }

        public object Execute(object input)
        {
            var type = Assembly.GetType("Runner");
            
            var obj = Activator.CreateInstance(type);
            var rst = type.InvokeMember("Run", BindingFlags.InvokeMethod, null, obj, new object[] { input }, null);
            return rst;
        }
    }
}