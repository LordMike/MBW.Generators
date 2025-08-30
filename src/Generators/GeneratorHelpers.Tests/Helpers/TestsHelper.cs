using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MBW.Generators.GeneratorHelpers.Attributes;
using MBW.Generators.GeneratorHelpers.Generator;
using MBW.Generators.Tests.Common;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.GeneratorHelpers.Tests.Helpers;

internal static class TestsHelper
{
    internal static async Task<(string? output, IReadOnlyList<Diagnostic> diags)> RunHelperAsync(string input, string[]? expectedDiagnostics = null)
    {
        var analysis = await GeneratorTestHelper.RunAnalyzer<SymbolExtensionsAnalyzer>(input,
            expectedDiagnostics ?? [],
            ["MBW.Generators.GeneratorHelpers.Attributes"], typeof(GenerateSymbolExtensionsAttribute));

        if (analysis.Length > 0)
            return (null, analysis);

        var (sources, diags) = GeneratorTestHelper.RunGenerator<SymbolExtensionsGenerator>(input,
            expectedDiagnostics ?? [],
            ["MBW.Generators.GeneratorHelpers.Attributes"], typeof(GenerateSymbolExtensionsAttribute));

        return sources.Count switch
        {
            0 => (null, diags),
            1 => (sources.First().Value, diags),
            _ => throw new InvalidOperationException($"Generator produced more than one file: {string.Join(", ", sources.Keys)}"),
        };
    }
}
