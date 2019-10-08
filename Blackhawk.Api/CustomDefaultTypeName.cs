using System.Collections.Generic;
using NJsonSchema;

namespace Blackhawk
{
    public class CustomDefaultTypeName : DefaultTypeNameGenerator
    {
        private readonly string _defaultName;

        public CustomDefaultTypeName(string defaultName)
        {
            _defaultName = defaultName;
        }

        public override string Generate(JsonSchema schema, string typeNameHint, IEnumerable<string> reservedTypeNames)
        {
            if (string.IsNullOrWhiteSpace(typeNameHint))
            {
                return _defaultName;
            }
            return base.Generate(schema, typeNameHint, reservedTypeNames);
        }
    }
}