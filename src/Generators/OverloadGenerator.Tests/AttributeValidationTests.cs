using System.Threading.Tasks;
using MBW.Generators.OverloadGenerator.Tests.Helpers;
using Xunit;

namespace MBW.Generators.OverloadGenerator.Tests;

public class AttributeValidationTests
{
    [Fact]
    public async Task InvalidRegex_Reported()
    {
        var diags = await TestsHelper.RunAnalyzer("""
                              using MBW.Generators.OverloadGenerator.Attributes;

                              [DefaultOverload("[", "1")]
                              public partial class Api { public void M(int a) { } }
                              """);
        await VerifyHelper.VerifyGeneratorAsync(null, diags);
    }

    [Fact]
    public async Task InvalidTransformExpression_Reported()
    {
        var diags = await TestsHelper.RunAnalyzer("""
                              using MBW.Generators.OverloadGenerator.Attributes;

                              [TransformOverload("id", typeof(int), "int.Parse({value}")]
                              public partial class Api { public void M(string id) { } }
                              """);
        await VerifyHelper.VerifyGeneratorAsync(null, diags);
    }
}
