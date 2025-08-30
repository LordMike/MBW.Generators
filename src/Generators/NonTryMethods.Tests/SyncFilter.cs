using System.Collections.Generic;
using System.Threading.Tasks;
using MBW.Generators.NonTryMethods.Generator;
using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.NonTryMethods.Tests;

public class SyncFilter
{
    [Fact]
    public async Task Bool_NoOut()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public bool TryMethod(int x) => true;
                                  }
                                  """);
        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.NotEligibleSync.Id, d.Id));
    }

    [Fact]
    public async Task Bool_TwoOuts()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public bool TryMethod(out int a, out int b) { a = 1; b = 2; return true; }
                                  }
                                  """);
        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.NotEligibleSync.Id, d.Id));
    }
}