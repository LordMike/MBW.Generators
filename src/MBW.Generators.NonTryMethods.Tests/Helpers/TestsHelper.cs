using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MBW.Generators.NonTryMethods.Abstracts.Attributes;
using MBW.Generators.Tests.Common;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.NonTryMethods.Tests.Helpers;

internal static partial class TestsHelper
{
    [GeneratedRegex(@"(\r\n|\n|\r)+")]
    private static partial Regex Newlines();

    internal static void CheckEqual(string expected, string? actual)
    {
        // Normalize
        Regex rgx = Newlines();
        expected = rgx.Replace(expected, "\n").Trim();
        actual = actual != null ? rgx.Replace(actual, "\n").Trim() : null;

        Assert.Equal(expected, actual);
    }

    internal static (string? output, IReadOnlyList<Diagnostic> diags) RunHelper(string input,
        string[]? expectedDiagnostics = null)
    {
        (IReadOnlyDictionary<string, string> sources, IReadOnlyList<Diagnostic> diags) =
            GeneratorTestHelper.Run<AutogenNonTryGenerator>(input, expectedDiagnostics ?? [],
                typeof(GenerateNonTryMethodAttribute));

        return sources.Count switch
        {
            0 => (null, diags),
            1 => (sources.First().Value, diags),
            _ => throw new InvalidOperationException(
                $"Generator produced more than one file: {string.Join(", ", sources.Keys)}"),
        };
    }
}