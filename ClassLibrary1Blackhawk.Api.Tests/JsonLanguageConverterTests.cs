using System;
using System.Linq;
using System.Runtime.InteropServices;
using Blackhawk;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Blackhawk
{
    public class JsonLanguageConverterTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public JsonLanguageConverterTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        JsonLanguageConverter SetupSut()
        { 
            return new JsonLanguageConverter(new JsonConvertionSettings().WithPascalCase(true));
        }

        [Fact]
        public void GenerateCsharp_WithMultiple_ReturnsExpectedResult()
        {
            var sut = SetupSut();
            sut.PrimaryClass = "Primary";
            var result = sut.GenerateCsharp("[ { \"name\" : { \"first\" : \"Jeppe\", \"last\" : \"Kristensen\"  }} ]");

            result.PrimaryClass.Should().Be("Primary");

            var compilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(result.Classes);
            var classes = compilationUnitSyntax.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().ToArray();

            classes.Length.Should().Be(2);
            classes.Should().Contain(x => x.Identifier.ToString() == "Primary");
            classes.Should().Contain(x => x.Identifier.ToString() == "Name");

        }
    }
} 