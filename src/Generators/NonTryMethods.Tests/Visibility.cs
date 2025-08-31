using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class Visibility
{
    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("private")]
    public async Task EmittedCode_UsesVisibilityOfSource_Method(string accessibility)
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync($$"""
                                    [GenerateNonTryMethod]
                                    internal partial class TestClass
                                    {
                                        {{accessibility}} bool TryMethod(out int value)
                                        {
                                            value = 0;
                                            return true;
                                        }
                                    }
                                    """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags, name: accessibility);
    }
    
    [Fact]
    public async Task EmittedCode_UsesVisibilityOfSource_InterfaceMethods()
    {
        // Interface methods must never have any visibility modifier, they are public by default.
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  
                                  [GenerateNonTryMethod]
                                  public partial interface TestInterface
                                  {
                                      bool TryMethod(out int v);
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}