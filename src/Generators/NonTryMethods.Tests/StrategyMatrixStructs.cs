using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class StrategyMatrixStructs
{
    [Fact]
    public async Task Auto_PartialInPlace_Struct()
    {
        // Should emit: error CS0260: Missing partial modifier on declaration of type 'TestClass'; another partial declaration of this type exists]
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.Auto)]
                                             public struct TestClass
                                             {
                                                 public bool TryMethod(out int v) { v = 3; return true; }
                                             }
                                             """, ["CS0260"]);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Extensions_Mutable_RefThisReceiver()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""

                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.Extensions)]
                                             public struct TestClass
                                             {
                                                 public bool TryMethod(out int v) { v = 4; return true; }
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Extensions_ReadonlyStruct_InThisReceiver()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""

                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.Extensions)]
                                             public readonly struct TestClass
                                             {
                                                 public bool TryMethod(out int v) { v = 5; return true; }
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task Extensions_StaticInput_Struct_ReportsCannotGenerateExtensionForStatic()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""

                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions(methodsGenerationStrategy: MethodsGenerationStrategy.Extensions)]
                                             public struct TestClass
                                             {
                                                 public static bool TryMethod(out int v) { v = 0; return false; }
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}