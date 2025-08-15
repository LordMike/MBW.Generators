using System.Collections.Generic;
using System.Text.RegularExpressions;
using MBW.Generators.NonTryMethods.Tests.Helpers;
using MBW.Generators.Tests.Common;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.NonTryMethods.Tests;

public class XmlDocCopyTests
{
    // (useNamespace, originalMethod signature, expected cref)
    [Theory]
    // Baseline
    [InlineData(false, "bool TryMethod(out string value)", "TestClass.TryMethod(out string)")]

    // Modifiers
    [InlineData(false, "bool TryMethod(in int x, out string value)", "TestClass.TryMethod(in int, out string)")]
    [InlineData(false, "bool TryMethod(ref int x, out string value)", "TestClass.TryMethod(ref int, out string)")]
    [InlineData(false, "bool TryMethod(out string value, params int[] xs)",
        "TestClass.TryMethod(out string, params int[])")]

    // Fully-qualified BCL types -> expect keyword in cref due to UseSpecialTypes
    [InlineData(false, "bool TryMethod(global::System.Int32 x, out string value)",
        "TestClass.TryMethod(int, out string)")]

    // Generic types with multiple type arguments
    [InlineData(false, "bool TryMethod(global::System.Func<int, string, bool> f, out string value)",
        "TestClass.TryMethod(Func<int, string, bool>, out string)")]

    // Mixed qualifiers & namespace
    [InlineData(true, "bool TryMethod(out string value)", "TestClass.TryMethod(out string)")]
    [InlineData(true,
        "bool TryMethod(ref global::System.Int32 x, in global::System.ReadOnlySpan<int> span, out string value)",
        "TestClass.TryMethod(ref int, in ReadOnlySpan<int>, out string)")]
    public void XmlDocs_ReferenceToOriginal(bool useNamespace, string originalMethod, string expected)
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            TestsHelper.RunHelper($$"""
                                    using MBW.Generators.NonTryMethods.Attributes;

                                    {{(useNamespace ? "namespace TestNamespace;" : "")}}

                                    [GenerateNonTryMethod]
                                    internal partial class TestClass
                                    {
                                        internal {{originalMethod}}
                                        {
                                            value = "ok";
                                            return true;
                                        }
                                    }
                                    """);

        Assert.Empty(diags);
        var seeString = Regex.Match(output, @"\<see cref=\""(.*?)\""/>");
        Assert.True(seeString.Success);

        var actual = seeString.Groups[1].Value;
        Assert.Equal(expected, actual);
    }
}