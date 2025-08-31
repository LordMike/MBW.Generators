using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class OutputNamespace
{
    [Fact]
    public async Task EmittedCode_HasNamespaceOfSource()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  namespace TestNamespace;

                                  [GenerateNonTryMethod]
                                  public partial class TestClass
                                  {
                                      public bool TryMethod(out int value)
                                      {
                                          value = 0;
                                          return true;
                                      }
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}