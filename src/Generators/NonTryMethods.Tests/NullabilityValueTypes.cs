using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class NullabilityValueTypes
{
    [Fact]
    public async Task Async_Verbatim_PreservesNullableT()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  using System.Threading.Tasks;
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public Task<(bool ok, int? value)> TryMethodAsync()
                                          => Task.FromResult((true, (int?)42));
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Async_TrueMeansNotNull_UnwrapsNullable()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  using System.Threading.Tasks;
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions(returnGenerationStrategy: ReturnGenerationStrategy.TrueMeansNotNull)]
                                  public partial class TestClass
                                  {
                                      public Task<(bool ok, int? value)> TryMethodAsync()
                                          => Task.FromResult((true, (int?)42));
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Sync_Verbatim_PreservesNullableT()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public bool TryMethod(out int? value)
                                      {
                                          value = 1;
                                          return true;
                                      }
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Sync_TrueMeansNotNull_UnwrapsNullable()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions(returnGenerationStrategy: ReturnGenerationStrategy.TrueMeansNotNull)]
                                  public partial class TestClass
                                  {
                                      public bool TryMethod(out int? value)
                                      {
                                          value = 1;
                                          return true;
                                      }
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}