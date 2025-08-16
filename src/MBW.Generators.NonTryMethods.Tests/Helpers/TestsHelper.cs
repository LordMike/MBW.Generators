using System;
using System.Collections.Generic;
using System.Linq;
using MBW.Generators.NonTryMethods.Attributes;
using MBW.Generators.Tests.Common;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests.Helpers;

internal static class TestsHelper
{
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