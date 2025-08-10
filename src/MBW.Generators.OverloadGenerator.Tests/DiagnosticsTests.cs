using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.OverloadGenerator.Tests;

public class DiagnosticsTests
{
    [Fact]
    public void Transform_MissingParameter_EmitsDiagnosticAndSkips()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using MBW.Generators.OverloadGenerator;
                                  using MBW.Generators.OverloadGenerator.Attributes;
                                  public enum K { A }

                                  public partial class X
                                  {
                                      [TransformOverload("nope", typeof(K), "{value}.ToString()")]
                                      public void M(string kind) { }
                                  }
                                  """);

        Assert.Contains(diags, d => d.Id == Diagnostics.MissingParameter.Id);
        Assert.Null(output);
    }

    [Fact]
    public void Transform_InvalidAcceptType_EmitsDiagnostic()
    {
        // Using typeof(void) for Transform should be rejected by the generator.
        (string? output, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using MBW.Generators.OverloadGenerator;
                                  using MBW.Generators.OverloadGenerator.Attributes;

                                  public partial class X2
                                  {
                                      [TransformOverload("k", typeof(void), "{value}")]
                                      public void M(string k) { }
                                  }
                                  """);

        Assert.Contains(diags, d => d.Id == Diagnostics.InvalidAcceptType.Id);
        Assert.Null(output);
    }

    [Fact]
    public void Transform_MissingValueToken_EmitsDiagnostic()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using MBW.Generators.OverloadGenerator;
                                  using MBW.Generators.OverloadGenerator.Attributes;
                                  public enum K { A }

                                  public partial class X3
                                  {
                                      [TransformOverload("k", typeof(K), "ToString()")] // no {value}
                                      public void M(string k) { }
                                  }
                                  """);

        Assert.Contains(diags, d => d.Id == Diagnostics.MissingValueToken.Id);
        Assert.Null(output);
    }

    [Fact]
    public void Default_MissingDefaultExpression_EmitsDiagnostic()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using MBW.Generators.OverloadGenerator;
                                  using MBW.Generators.OverloadGenerator.Attributes;

                                  public partial class X4
                                  {
                                      [DefaultOverload("k", "")]
                                      public void M(string k) { }
                                  }
                                  """);

        Assert.Contains(diags, d => d.Id == Diagnostics.MissingDefaultExpression.Id);
        Assert.Null(output);
    }

    [Fact]
    public void Collision_GeneratedSignatureConflict_IsReportedAndSkipped()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper("""
                                  using MBW.Generators.OverloadGenerator;
                                  using MBW.Generators.OverloadGenerator.Attributes;
                                  public enum K { A }

                                  public partial class X5
                                  {
                                      [DefaultOverload("k", "\"A\"")]
                                      public void M(string k, int n) { }

                                      public void M(int n) { } // collides with removal
                                  }
                                  """);

        Assert.Contains(diags, d => d.Id == Diagnostics.SignatureCollision.Id);
        Assert.Null(output);
    }
}