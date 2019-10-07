using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Blackhawk.Extensions;
using FluentAssertions;
using Namotion.Reflection;
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
        public async Task BuildSingleJsonReplReturnsExpected()
        {
            (object obj, _) = await Build
                .Init()
                .WithConverter(new JsonLanguageConverter(new JsonConvertionSettings()))
                .GenerateSource("{ \"name\" : { \"firstName\" : \"Jeppe Kristensen\"}}")
                .Repl()
                .Execute("return input.Name.FirstName;");
            
            obj.Should().NotBeNull();
            dynamic dynamicObj = obj;
            
            obj.Should().Be("Jeppe Kristensen");
        }

        [Fact]
        public async Task BuildEnumerableJsonReplReturnsExpected()
        {
            (object obj, _) = await Build
                .Init()
                .WithConverter(new JsonLanguageConverter(new JsonConvertionSettings()))
                .GenerateSource("[{ \"name\" : { \"firstName\" : \"Jeppe Kristensen\"}}]")
                .Repl()
                .Execute("return input;");

            obj.Should().NotBeNull();
            dynamic dynamicObj = obj;

            var count = (int) dynamicObj.Count;
            count.Should().Be(1);

            var firstName = (string) dynamicObj[0].Name.FirstName;
            firstName.Should().Be("Jeppe Kristensen");

        }
    }
}
    
