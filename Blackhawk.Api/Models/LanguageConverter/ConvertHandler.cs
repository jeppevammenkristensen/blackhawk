using System;
using System.Collections.Generic;
using System.Linq;

namespace Blackhawk.Models.LanguageConverter
{
    //public class ConvertHandler
    //{
    //    private readonly IEnumerable<ILanguageConverter> _converters;

    //    public ConvertHandler(IEnumerable<ILanguageConverter> converters)
    //    {
    //        _converters = converters;
    //    }

    //    public ConvertInformation GenerateConvertInformation(string source, SourceLanguage language,
    //        IConvertionSettings settings)
    //    {
    //        var converter = _converters.FirstOrDefault(x => x.Source == language);
    //        if (converter == null)
    //            return ConvertInformation.Failure(ConvertStatus.UnsupportedSource);

    //        var check = converter.InputIsValid(source,settings);
    //        if (check.success == false)
    //        {
    //            return ConvertInformation.Failure(ConvertStatus.InvalidSource, check.details);
    //        }

    //        try
    //        {
    //            var result = converter.GenerateCsharp(source,settings);
    //            return ConvertInformation.Success(result,converter);
    //        }
    //        catch (Exception exception)
    //        {
    //            return ConvertInformation.Failure(ConvertStatus.Failed, exception.Message);
    //        }
    //    }
    //}

   
}