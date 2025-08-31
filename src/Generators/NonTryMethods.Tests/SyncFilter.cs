using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class SyncFilter
{
    [Fact]
    public async Task Bool_NoOut()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public bool TryMethod(int x) => true;
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Bool_TwoOuts()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public bool TryMethod(out int a, out int b) { a = 1; b = 2; return true; }
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}