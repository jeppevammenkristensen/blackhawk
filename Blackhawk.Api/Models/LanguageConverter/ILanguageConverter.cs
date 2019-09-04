using System;

namespace Blackhawk.Models.LanguageConverter
{
    public interface IConvertionSettings
    {

    }

    /// <summary>
    /// The result of the conversion
    /// </summary>
    public class Conversion
    {
        public Conversion(string primaryClass, string classes, bool inputIsEnumerable)
        {
            PrimaryClass = primaryClass;
            Classes = classes;
            InputIsEnumerable = inputIsEnumerable;
        }

        /// <summary>
        /// The name of the primary class (root)
        /// </summary>
        public string PrimaryClass { get; private set; }
        /// <summary>
        /// The classes generated. 
        /// </summary>
        public string Classes { get; private set; }

        public bool InputIsEnumerable { get; private set; }

        //public string InputParameterType { get; set; }
    }

    public interface ILanguageConverter
    {
        /// <summary>
        /// Validates if the passed input is valid
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        (bool success, string details) InputIsValid(string source);

        /// <summary>
        /// Generate csharp classes
        /// </summary>
        /// <param name="input"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        Conversion GenerateCsharp(string input);
        //object Deserialize(Type type, string requestSource);
    }
}