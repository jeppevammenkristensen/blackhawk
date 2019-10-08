using System.Reflection;

namespace Blackhawk
{
    public class Reference
    {
        public readonly Assembly _assembly;
        public readonly string[] _usings;

        public Reference(Assembly assembly, params string[] usings)
        {
            _assembly = assembly;
            _usings = usings;
        }
    }
}