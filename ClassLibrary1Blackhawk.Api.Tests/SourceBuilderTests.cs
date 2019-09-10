using System;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Blackhawk
{
    public class SourceBuilderTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SourceBuilderTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Can_Build_Json_From_Fluent_Interface()
        {
            var builder = Build.Init().WithConverter(new JsonLanguageConverter(new JsonConvertionSettings()));
            var source = builder.GenerateCsharp("[{ \"name\" : \"Jeppe Kristensen\"}]");
            source.PrimarySource.Identifier.ToString().Should().Be("ReturnObject");

            var result = source.BuildCompilation;
            _testOutputHelper.WriteLine(result.ToString());
            int i = 0;


        }

    }
}