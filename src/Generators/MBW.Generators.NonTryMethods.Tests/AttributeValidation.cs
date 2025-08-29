using System.Collections.Generic;
using System.Threading.Tasks;
using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.NonTryMethods.Tests;

public class AttributeValidation
{
    [Fact]
    public async Task MRegularExpressionIsInvalid_Reported()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
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
        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.RegularExpressionIsInvalid.Id, d.Id));
    }
}