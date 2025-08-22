using System.Collections.Generic;
using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.NonTryMethods.Tests;

public class AttributeValidation
{
    [Fact]
    public void MRegularExpressionIsInvalid_Reported()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
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