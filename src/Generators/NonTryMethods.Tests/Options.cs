using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class Options
{
    [Fact]
    public async Task TypeBeatsAssembly()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  using System.Threading.Tasks;
                                  
                                  [assembly: GenerateNonTryOptions(returnGenerationStrategy: ReturnGenerationStrategy.Verbatim)]

                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions(returnGenerationStrategy: ReturnGenerationStrategy.TrueMeansNotNull)]
                                  public partial class TestClass
                                  {
                                      public Task<(bool ok, string? value)> TryMethodAsync()
                                          => Task.FromResult((true, (string?)"x"));
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}