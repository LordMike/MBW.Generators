using System.Collections.Generic;
using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.NonTryMethods.Tests;

public class Collisions
{
    [Fact]
    public void GeneratedVsExisting_SignatureCollision()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using MBW.Generators.NonTryMethods.Attributes;

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
    public void MultipleAttributes_DuplicateSignature()
    {
        (string? _, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using System.Threading.Tasks;
                                  using MBW.Generators.NonTryMethods.Attributes;

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