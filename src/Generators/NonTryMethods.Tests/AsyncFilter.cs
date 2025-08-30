using System.Collections.Generic;
using System.Threading.Tasks;
using MBW.Generators.NonTryMethods.Generator;
using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.NonTryMethods.Tests;

public class AsyncFilter
{
    [Fact]
    public async Task WrongTupleOrder()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
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
    }

    [Fact]
    public async Task BoolOnlyTask()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
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
        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.NotEligibleAsyncShape.Id, d.Id));
    }

    [Fact]
    public async Task NonTupleValueTask()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
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
        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.NotEligibleAsyncShape.Id, d.Id));
    }

    [Fact]
    public async Task AsyncDisabledByOptions()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
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
        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.NotEligibleAsyncShape.Id, d.Id));
    }
}