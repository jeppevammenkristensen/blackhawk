using System.Collections.Generic;
using System.Dynamic;

namespace Blackhawk.Models.LanguageConverter
{
    /// <summary>
    /// The result of the conversion
    /// </summary>
    public class Conversion
    {
        public HashSet<Reference> References { get;  } = new HashSet<Reference>();
        
        public Conversion(string primaryClass, string classes, bool inputIsEnumerable, string sourceConverter)
        {
            PrimaryClass = primaryClass;
            Classes = classes;
            InputIsEnumerable = inputIsEnumerable;
            SourceConverter = sourceConverter;
        }

        /// <summary>
        /// The name of the primary class (root)
        /// </summary>
        public string PrimaryClass { get; }
        /// <summary>
        /// The classes generated. 
        /// </summary>
        public string Classes { get; }

        public bool InputIsEnumerable { get; }

        public string SourceConverter { get; }

        public Conversion WithReference(Reference reference)
        {
            References.Add(reference);
            return this;
        }
    }
}