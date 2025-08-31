using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class Patterns
{
    [Fact]
    public async Task DefaultRegex_RemovesTryPrefix()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                             using System.Threading.Tasks;

                                             [GenerateNonTryMethod] // default ^[Tt]ry(.*)
                                             [GenerateNonTryOptions]
                                             public partial class TestClass
                                             {
                                                 public Task<(bool ok, string? value)> TryMethodAsync()
                                                     => Task.FromResult((true, "x"));
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task MultiplePatterns_SameSignature_Dedup()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                             using System.Threading.Tasks;

                                             [GenerateNonTryMethod(methodNamePattern: "^[Tt]ry(.*)$")]
                                             [GenerateNonTryMethod(methodNamePattern: "^Try(.*)$")]
                                             [GenerateNonTryOptions]
                                             public partial class TestClass
                                             {
                                                 public Task<(bool ok, string? value)> TryMethodAsync()
                                                     => Task.FromResult((true, "x"));
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}