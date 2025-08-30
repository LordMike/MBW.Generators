using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MBW.Generators.NonTryMethods.Attributes;
using MBW.Generators.NonTryMethods.Generator;
using MBW.Generators.Tests.Common;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests.Helpers;

internal static class TestsHelper
{
    internal static async Task<(string? output, IReadOnlyList<Diagnostic> diags)> RunHelperAsync(string input,
        string[]? expectedDiagnostics = null)
    {
        var analysis =
            await GeneratorTestHelper.RunAnalyzer<NonTryAttributeValidatorAnalyzer>(input, expectedDiagnostics ?? [],
                ["MBW.Generators.NonTryMethods.Attributes"], typeof(GenerateNonTryMethodAttribute));

        if (analysis.Length > 0)
            return (null, analysis);

        (IReadOnlyDictionary<string, string> sources, IReadOnlyList<Diagnostic> diags) =
            GeneratorTestHelper.RunGenerator<NonTryGenerator>(input, expectedDiagnostics ?? [],
                ["MBW.Generators.NonTryMethods.Attributes"], typeof(GenerateNonTryMethodAttribute));

        return sources.Count switch
        {
            0 => (null, diags),
            1 => (sources.First().Value, diags),
            _ => throw new InvalidOperationException(
                $"Generator produced more than one file: {string.Join(", ", sources.Keys)}"),
        };
    }
}