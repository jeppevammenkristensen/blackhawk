namespace Blackhawk.Models.LanguageConverter
{

    /// <summary>
    /// The status of a source convertion. Values can be
    /// Success, InvalidSource, Failed or Unsupportedsource
    /// </summary>
    public enum ConvertStatus
    {
        Success,
        InvalidSource, // The source is not valid
        Failed, // A failure occcurred
        UnsupportedSource // No converter found
    }
}