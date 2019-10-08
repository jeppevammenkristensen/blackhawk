using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blackhawk.Extensions
{
    public static class SerializerExtensions
    {
        public static string ToJson(this (object? result, CompiledCode? code) source)
        {
            var (result, compiledCode) = source;
            if (result != null)
            {

                return JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }

            if (compiledCode != null)
            {
                return JsonSerializer.Serialize(new
                {
                    Diagnostics = compiledCode.Diagnostics.Select(x => new
                    {
                        Message = x.GetMessage(),
                        Severity = x.Severity.ToString()
                    })
                }, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }

            return "null";
        }
        public static async Task<string> ToJson(this Task<(object? result, CompiledCode? code)> source)
        {
            var result = await source;
            return ToJson(result);
        }
    }
}