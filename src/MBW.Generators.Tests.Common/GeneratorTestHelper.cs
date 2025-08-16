using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MBW.Generators.Tests.Common;

public static class GeneratorTestHelper
{
    public static (IReadOnlyDictionary<string, string> Sources, IReadOnlyList<Diagnostic> Diagnostics) Run<TGenerator>(
        string source, string[] expectedDiagnostics, params Type[] assembliesToReference)
        where TGenerator : IIncrementalGenerator, new()
    {
        SyntaxTree globalUsingsTree = CSharpSyntaxTree.ParseText(
            "global using System;",
            new CSharpParseOptions(LanguageVersion.Latest));

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

        PortableExecutableReference[] refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Concat(assembliesToReference.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location)))
            .DistinctBy(s => s.FilePath)
            .ToArray();

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Tests",
            new[] { globalUsingsTree, syntaxTree },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var syntaxDiagnostics = compilation.GetDiagnostics()
            .Where(s => !expectedDiagnostics.Contains(s.Id))
            .Where(s => s.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (syntaxDiagnostics.Length > 0)
            throw new InvalidOperationException("Syntax error in test:\n" +
                                                string.Join("\n", syntaxDiagnostics.Select(x => x.ToString())));

        IIncrementalGenerator generator = new TGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();

        Dictionary<string, string> sources =
            result.GeneratedSources.ToDictionary(x => x.HintName, x => x.SourceText.ToString());
        ImmutableArray<Diagnostic> diags = result.Diagnostics;
        return (sources, diags);
    }
}