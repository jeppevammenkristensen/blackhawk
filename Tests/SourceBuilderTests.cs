using System.Threading.Tasks;
using Blackhawk;
using Blackhawk.Extensions;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class SourceBuilderTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SourceBuilderTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task BuildSingleJsonReplReturnsExpected()
        {
            var  output = await Build
                .Init()
                .WithConverter(new JsonLanguageConverter(new JsonConvertionSettings()))
                .GenerateSourceFromString("{ \"name\" : { \"firstName\" : \"Jeppe Kristensen\"}}")
                .Repl()
                .Execute("return input;").ToJson();
            
            _testOutputHelper.WriteLine(output);
        }

        [Fact]
        public async Task BuildEnumerableJsonReplReturnsExpected()
        {
            (object obj, _) = await Build
                .Init()
                .WithConverter(new JsonLanguageConverter(new JsonConvertionSettings()))
                .GenerateSourceFromString("[{ \"name\" : { \"firstName\" : \"Jeppe Kristensen\"}}]")
                .Repl()
                .Execute("return input;");

            obj.Should().NotBeNull();
            dynamic dynamicObj = obj;

            var count = (int) dynamicObj.Count;
            count.Should().Be(1);

            var firstName = (string) dynamicObj[0].Name.FirstName;
            firstName.Should().Be("Jeppe Kristensen");

        }

        [Fact]
        public async Task BuildCsvResultReturnsExpected()
        {
            (object result, CompiledCode code) valueTuple = await Build.Init()
                .WithConverter(new CsvLanguageConverter(new CsvConvertionSettings()
                {
                    Delimiter = ",",
                    FirstLineContainsHeaders = true
                }))
                .GenerateSourceFromString(@"Firstname,age
""Jeppe"",41").Repl().Execute("return input.Select(x => new { x.Firstname }).First();");
            _testOutputHelper.WriteLine(valueTuple.ToJson());
        }

        [Fact]
        public async Task CanInitSourceFromFile()
        {
            var source = await Build
                .Init()
                .WithConverter(new JsonLanguageConverter(new JsonConvertionSettings()))
                .GenerateSourceFromFile("TestArtifacts/TestFile.json");
            source.ClassSources.Should().Contain(x => x.Name == "ReturnObject");
            source.ClassSources.Should().Contain(x => x.Name == "Parents");


        }
    }
}
    
