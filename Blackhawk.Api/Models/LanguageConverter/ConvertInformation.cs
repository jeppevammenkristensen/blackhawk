using System;

namespace Blackhawk.Models.LanguageConverter
{
    //public class ConvertInformation
    //{
    //    public ConvertInformation()
    //    {
            
    //    }

    //    /// <summary>
    //    /// The classes generated
    //    /// </summary>
    //    public string Classes { get; set; }

    //    public string PrimaryClass { get; set; }
    //    public ConvertStatus ConvertStatus { get; set; }
    //    public ILanguageConverter Converter { get; private set; }

    //    public string InputParameterType { get; set; }

    //    public static ConvertInformation Success(Conversion conversion, ILanguageConverter converter)
    //    {
    //        return new ConvertInformation()
    //        {
    //            Classes = conversion.Classes,
    //            ConvertStatus = ConvertStatus.Success,
    //            Converter = converter,
    //            PrimaryClass = conversion.PrimaryClass,
    //            InputParameterType = null; //conversion.InputParameterType
    //        };
    //    }

    //    public static ConvertInformation Failure(ConvertStatus status, string errorDetails = null)
    //    {
    //        if (status == ConvertStatus.Success)
    //            throw new InvalidOperationException($"Method should only be used to create a failure object. {nameof(ConvertStatus.Success)} was passed");

    //        return new ConvertInformation()
    //        {
    //            ConvertStatus = status,
    //            ErrorDetails = errorDetails
    //        };
    //    }

    //    public string ErrorDetails { get; set; }

    //}
}