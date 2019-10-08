namespace Blackhawk.Models.LanguageConverter
{
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