using MBW.Generators.NonTryMethods.Generator;
using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class AsyncFilter
{
    [Fact]
    public async Task WrongTupleOrder()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                             using System.Threading.Tasks;

                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions]
                                             public partial class TestClass
                                             {
                                                 public Task<(int v, bool ok)> TryMethodAsync()
                                                     => Task.FromResult((1, true));
                                             }
                                             """);

        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.NotEligibleAsyncShape.Id, d.Id));
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task BoolOnlyTask()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                             using System.Threading.Tasks;

                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions]
                                             public partial class TestClass
                                             {
                                                 public Task<bool> TryMethodAsync()
                                                     => Task.FromResult(true);
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task NonTupleValueTask()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                             using System.Threading.Tasks;

                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions]
                                             public partial class TestClass
                                             {
                                                 public ValueTask<string?> TryMethodAsync()
                                                     => new ValueTask<string?>("x");
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task AsyncDisabledByOptions()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                             using System.Threading.Tasks;

                                             [GenerateNonTryMethod]
                                             [GenerateNonTryOptions(asyncCandidateStrategy: AsyncCandidateStrategy.None)]
                                             public partial class TestClass
                                             {
                                                 public Task<(bool ok, string? v)> TryMethodAsync()
                                                     => Task.FromResult((true, "x"));
                                             }
                                             """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}