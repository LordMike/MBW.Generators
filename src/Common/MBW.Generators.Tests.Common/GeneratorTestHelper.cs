using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MBW.Generators.Tests.Common;

public static class GeneratorTestHelper
{
    public static (IReadOnlyDictionary<string, string> Sources, IReadOnlyList<Diagnostic> Diagnostics) Run<TGenerator>(
        string source, string[] expectedDiagnostics, string[] defaultUsings, params Type[] assembliesToReference)
        where TGenerator : IIncrementalGenerator, new()
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
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Ignore expected syntax errors, and:
        // CS0122 (attribute is inaccessible due to protection level) -- we first emit attributes later on
        var syntaxDiagnostics = compilation.GetDiagnostics()
            .Where(s => !expectedDiagnostics.Contains(s.Id) && s.Id != "CS0122")
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
            result.GeneratedSources
                .Where(s => s.HintName != "Microsoft.CodeAnalysis.EmbeddedAttribute.cs" &&
                            !s.HintName.StartsWith("MBW.Generators.", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.HintName, x => x.SourceText.ToString());

        return (sources, result.Diagnostics);
    }
}