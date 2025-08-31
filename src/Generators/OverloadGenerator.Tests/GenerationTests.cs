using System.Collections.Generic;
using System.Threading.Tasks;
using MBW.Generators.OverloadGenerator.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.OverloadGenerator.Tests;

public class GenerationTests
{
    [Fact]
    public async Task DefaultOverload_Generates()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper.RunHelper("""
                              using MBW.Generators.OverloadGenerator;
                              using MBW.Generators.OverloadGenerator.Attributes;

                              [DefaultOverload("retry", "true")]
                              public partial class Api
                              {
                                  public void Call(string path, bool retry) { }
                              }
                              """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task TransformOverload_Generates()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper.RunHelper("""
                              using MBW.Generators.OverloadGenerator;
                              using MBW.Generators.OverloadGenerator.Attributes;

                              [TransformOverload("id", typeof(string), "int.Parse({value})")]
                              public partial class Api2
                              {
                                  public void Find(int id) { }
                              }
                              """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task SignatureCollision_Reported()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper.RunHelper("""
                              using MBW.Generators.OverloadGenerator;
                              using MBW.Generators.OverloadGenerator.Attributes;

                              [DefaultOverload("retry", "true")]
                              public partial class Api3
                              {
                                  public void Call(string path, bool retry) { }
                                  public void Call(string path) { }
                              }
                              """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}
