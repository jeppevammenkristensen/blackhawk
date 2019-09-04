using System;
using System.Collections.Generic;
using System.IO;
using Blackhawk.LanguageConverters.Json;
using Blackhawk.Models.LanguageConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Blackhawk
{
    public class JsonConvertionSettings
    {
        public bool UsePascalCase { get; set; } = true;

        public JsonConvertionSettings WithPascalCase(bool usePascalCase)
        {
            this.UsePascalCase = usePascalCase;
            return this;
        }
    }

    public class JsonLanguageConverter : ILanguageConverter
    {
        private readonly JsonConvertionSettings _settings;

        public JsonLanguageConverter(JsonConvertionSettings settings)
        {
            _settings = settings;
        }

        public (bool success, string details) InputIsValid(string source)
        {
            try
            {
                JToken.Parse(source);
                return (true, null);
            }
            catch (JsonReaderException e)
            {
                return (false, e.Message);
            }
        }

        public Conversion GenerateCsharp(string input)
        {
            var jsonClassHelper = new JsonClassGenerator();
            var writer = new StringWriter();

            jsonClassHelper.Example = input;
            jsonClassHelper.MainClass = PrimaryClass;
            jsonClassHelper.OutputStream = writer;
            jsonClassHelper.UseProperties = true;
            jsonClassHelper.UsePascalCase = _settings.UsePascalCase;

            jsonClassHelper.GenerateClasses();

            return new Conversion(writer.ToString(), PrimaryClass, true);

        }

        public string PrimaryClass => "ReturnObject";

        public object Deserialize(Type parameterType, string requestSource)
        {
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(parameterType);

            var result = JToken.Parse(requestSource);
            if (result is JObject jObject)
            {
                var arr = new JArray { jObject };
                requestSource = arr.ToString();
            }

            return JsonConvert.DeserializeObject(
                requestSource, enumerableType,
                new JsonSerializerSettings()
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
        }
    }
}
