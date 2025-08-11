using System;
using System.Collections.Generic;
using System.Linq;
using MBW.Generators.OverloadGenerator.Attributes;
using MBW.Generators.Tests.Common;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Tests;

internal static class TestsHelper
{
    internal static (string? output, IReadOnlyList<Diagnostic> diags) RunHelper(string input,
        string[]? expectedDiagnostics = null)
    {
        (IReadOnlyDictionary<string, string> sources, IReadOnlyList<Diagnostic> diags) =
            GeneratorTestHelper.Run<OverloadGenerator>(input, expectedDiagnostics ?? [],
                typeof(DefaultOverloadAttribute));

        return sources.Count switch
        {
            0 => (null, diags),
            1 => (sources.First().Value, diags),
            _ => throw new InvalidOperationException(
                $"Generator produced more than one file: {string.Join(", ", sources.Keys)}"),
        };
    }
}