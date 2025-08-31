using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class AttributeValidation
{
    [Fact]
    public async Task MRegularExpressionIsInvalid_Reported()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  [GenerateNonTryMethod(methodNamePattern: "^(Try)(.*)$")]
                                  
                                  public class TestClass
                                  {
                                      public bool TryMethod(out int a)
                                      {
                                        a = 0;
                                        return true;
                                      }
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}