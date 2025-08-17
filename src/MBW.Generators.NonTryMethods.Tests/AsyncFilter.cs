using System.Collections.Generic;
using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.NonTryMethods.Tests;

public class AsyncFilter
{
    [Fact]
    public void WrongTupleOrder()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using System.Threading.Tasks;
                                  using MBW.Generators.NonTryMethods.Attributes;

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
    public void BoolOnlyTask()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using System.Threading.Tasks;
                                  using MBW.Generators.NonTryMethods.Attributes;

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
    public void NonTupleValueTask()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using System.Threading.Tasks;
                                  using MBW.Generators.NonTryMethods.Attributes;

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
    public void AsyncDisabledByOptions()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using System.Threading.Tasks;
                                  using MBW.Generators.NonTryMethods.Attributes;

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