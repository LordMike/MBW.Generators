using System.Collections.Generic;
using System.Threading.Tasks;
using MBW.Generators.NonTryMethods.Generator;
using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.NonTryMethods.Tests;

public class Collisions
{
    [Fact]
    public async Task GeneratedVsExisting_SignatureCollision()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public bool TryMethod(out int v) { v = 1; return true; }

                                      // This collides with the would-be generated Method()
                                      public int Method() => 0;
                                  }
                                  """);
        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.SignatureCollision.Id, d.Id));
    }

    [Fact]
    public async Task MultipleAttributes_DuplicateSignature()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  using System.Threading.Tasks;
                                  
                                  [GenerateNonTryMethod(methodNamePattern: "^Try(.*)$")]
                                  [GenerateNonTryMethod(methodNamePattern: "^[Tt]ry(.*)$")]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public Task<(bool ok, string? value)> TryMethodAsync()
                                          => Task.FromResult((true, "x"));
                                  }
                                  """);
        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.MultiplePatternsMatchMethod.Id, d.Id));
    }
}