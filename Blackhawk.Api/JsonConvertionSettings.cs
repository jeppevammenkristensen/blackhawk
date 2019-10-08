namespace Blackhawk
{
    public class JsonConvertionSettings
    {
        public bool UsePascalCase { get; set; } = true;

        public JsonConvertionSettings WithPascalCase(bool usePascalCase)
        {
            UsePascalCase = usePascalCase;
            return this;
        }
    }
}