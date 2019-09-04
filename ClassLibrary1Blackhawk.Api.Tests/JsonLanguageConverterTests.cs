using System.Runtime.InteropServices;
using Blackhawk;

namespace Blackhawk
{
    public class JsononLanguageConverterTests
    {
        JsonLanguageConverter SetupSut()
        {
            return new JsonLanguageConverter(new JsonConvertionSettings());
        }
    }
}