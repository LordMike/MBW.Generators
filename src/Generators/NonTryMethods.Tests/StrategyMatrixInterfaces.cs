using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class StrategyMatrixInterfaces
{
    [Fact]
    public async Task Auto_DefaultImplementation_InInterface()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.Auto)]
                                  public partial interface TestInterface
                                  {
                                      bool TryMethod(out int v);
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task PartialType_DefaultImplementation_InInterface()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.PartialType)]
                                  public partial interface TestInterface
                                  {
                                      bool TryMethod(out int v);
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Extensions_Interface_GeneratesExtension()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.Extensions)]
                                  public interface TestInterface
                                  {
                                      bool TryMethod(out int v);
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}