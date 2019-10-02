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
        public async Task Can_Build_Json_From_Fluent_Interface()
        {
            var builder = Build.Init().WithConverter(new JsonLanguageConverter(new JsonConvertionSettings()));
            var source = builder.GenerateSource("[{ \"name\" : { \"firstName\" : \"Jeppe Kristensen\"}}]");
            source.PrimarySource.Identifier.ToString().Should().Be("ReturnObject");

            var nameClass = source.ClassSources.FirstOrDefault(x => x.Name == "Name");

            var (obj, code) = await source.Repl().Execute("return input;");
            obj.Should().NotBeNull();
            obj.Should().Be(5);
            // //var result = source.ExecuteAsync("return input;");
            // var result = source.BuildBaseCompilation();
            // var item = source.Compile(result);
            //tem.Success.Should().BeTrue();


            //item.Diagnostics.Should().HaveCount(1);


            //var mainType = item.Assembly.GetType("Runner");
            //var methodInfo = mainType.GetMethod("RunAsync");
            //var task = (Task<object>)methodInfo.Invoke(null, new object[] {null});
            //var res = await task;

            //res.Should().Be(5);
            //mainType.Should().NotBeNull();
            //_testOutputHelper.WriteLine(result.ToString());

            //int i = 0;


        }

    }
}
    
