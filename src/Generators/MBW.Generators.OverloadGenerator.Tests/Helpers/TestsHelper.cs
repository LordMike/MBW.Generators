using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MBW.Generators.OverloadGenerator.Attributes;
using MBW.Generators.Tests.Common;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Tests.Helpers;

internal static class TestsHelper
{
    internal static (string? output, IReadOnlyList<Diagnostic> diags) RunHelper(string input,
        string[]? expectedDiagnostics = null)
    {
        (IReadOnlyDictionary<string, string> sources, IReadOnlyList<Diagnostic> diags) =
            GeneratorTestHelper.RunGenerator<OverloadGenerator>(input, expectedDiagnostics ?? [],
                ["MBW.Generators.OverloadGenerator.Attributes"],
                typeof(DefaultOverloadAttribute));

        return sources.Count switch
        {
            0 => (null, diags),
            1 => (sources.First().Value, diags),
            _ => throw new InvalidOperationException(
                $"Generator produced more than one file: {string.Join(", ", sources.Keys)}"),
        };
    }

    internal static Task<ImmutableArray<Diagnostic>> RunAnalyzer(string input,
        string[]? expectedDiagnostics = null) =>
        GeneratorTestHelper.RunAnalyzer<OverloadAttributeValidatorAnalyzer>(input, expectedDiagnostics ?? [],
            ["MBW.Generators.OverloadGenerator.Attributes"], typeof(DefaultOverloadAttribute));
}