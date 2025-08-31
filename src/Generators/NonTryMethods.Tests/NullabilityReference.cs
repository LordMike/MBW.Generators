using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class NullabilityReference
{
    [Fact]
    public async Task Async_Verbatim_PreservesQuestion()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  using System.Threading.Tasks;
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public Task<(bool ok, string? value)> TryMethodAsync()
                                          => Task.FromResult((true, (string?)null));
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Async_TrueMeansNotNull_StripsQuestion()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  using System.Threading.Tasks;
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions(returnGenerationStrategy: ReturnGenerationStrategy.TrueMeansNotNull)]
                                  public partial class TestClass
                                  {
                                      public Task<(bool ok, string? value)> TryMethodAsync()
                                          => Task.FromResult((true, "hi"));
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Sync_Verbatim_PreservesQuestion()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public bool TryMethod(out string? value)
                                      {
                                          value = null;
                                          return true;
                                      }
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Sync_TrueMeansNotNull_StripsQuestion()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions(returnGenerationStrategy: ReturnGenerationStrategy.TrueMeansNotNull)]
                                  public partial class TestClass
                                  {
                                      public bool TryMethod(out string? value)
                                      {
                                          value = "x";
                                          return true;
                                      }
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}