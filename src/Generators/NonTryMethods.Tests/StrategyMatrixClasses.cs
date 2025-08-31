using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class StrategyMatrixClasses
{
    [Fact]
    public async Task Auto_PartialInPlace()
    {
        // Should emit: error CS0260: Missing partial modifier on declaration of type 'TestClass'; another partial declaration of this type exists]
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.Auto)]
                                             public class TestClass
                                             {
                                                 public bool TryMethod(out int v) { v = 1; return true; }
                                             }
                                             """, ["CS0260"]);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Partial_ExplicitInPlace()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.PartialType)]
                                             public partial class TestClass
                                             {
                                                 public bool TryMethod(out int v) { v = 1; return true; }
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Extensions_InstanceReceiver()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.Extensions)]
                                             public class TestClass
                                             {
                                                 public bool TryMethod(out int v) { v = 2; return true; }
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Extensions_StaticInput_ReportsCannotGenerateExtensionForStatic()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""

                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.Extensions)]
                                             public class TestClass
                                             {
                                                 public static bool TryMethod(out int v) { v = 0; return false; }
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}