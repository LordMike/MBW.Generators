using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MBW.Generators.Tests.Common;

public static class GeneratorTestHelper
{
    public static async Task<ImmutableArray<Diagnostic>> RunAnalyzer<TAnalyzer>(
        string source, string[] expectedDiagnostics, string[] defaultUsings, params Type[] assembliesToReference)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var compilation = CreateCompilation(source, defaultUsings, assembliesToReference);

        VerifySyntaxDiagnostics(compilation, expectedDiagnostics);

        var analyzer = new TAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    public static (IReadOnlyDictionary<string, string> Sources, IReadOnlyList<Diagnostic> Diagnostics)
        RunGenerator<TGenerator>(
            string source, string[] expectedDiagnostics, string[] defaultUsings, params Type[] assembliesToReference)
        where TGenerator : IIncrementalGenerator, new()
    {
        var compilation = CreateCompilation(source, defaultUsings, assembliesToReference);

        // VerifySyntaxDiagnostics(compilation, expectedDiagnostics);

        IIncrementalGenerator generator = new TGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        // driver = driver.RunGenerators(compilation);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out _);

        VerifySyntaxDiagnostics(newCompilation, expectedDiagnostics);

        GeneratorRunResult result = driver.GetRunResult().Results.Single();

        Dictionary<string, string> sources =
            result.GeneratedSources
                .Where(s => s.HintName != "Microsoft.CodeAnalysis.EmbeddedAttribute.cs" &&
                            !s.HintName.StartsWith("MBW.Generators.", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.HintName, x => x.SourceText.ToString());

        return (sources,
            result.Diagnostics
                .Concat(newCompilation.GetDiagnostics().Where(s => s.Severity == DiagnosticSeverity.Error)).ToArray());
    }

    private static CSharpCompilation CreateCompilation(string source,
        string[] defaultUsings, Type[] assembliesToReference)
    {
        SyntaxTree globalUsings = CSharpSyntaxTree.ParseText(
            string.Join("\n", defaultUsings.Append("System").Select(s => $"global using {s};")),
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
            new[] { globalUsings, syntaxTree },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        return compilation;
    }

    private static void VerifySyntaxDiagnostics(Compilation compilation, string[] expectedDiagnostics)
    {
        var syntaxDiagnostics = compilation.GetDiagnostics()
            .Where(s => !expectedDiagnostics.Contains(s.Id))
            .Where(s => s.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (syntaxDiagnostics.Length > 0)
            throw new InvalidOperationException("Syntax error in test:\n" +
                                                string.Join("\n", syntaxDiagnostics.Select(x => x.ToString())));
    }
}