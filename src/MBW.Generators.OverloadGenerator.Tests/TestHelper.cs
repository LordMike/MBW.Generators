using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MBW.Generators.OverloadGenerator.Tests;

internal static class TestHelper
{
    public static (IReadOnlyDictionary<string, string> Sources, IReadOnlyList<Diagnostic> Diagnostics) Run(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DefaultOverloadAttribute).Assembly.Location)
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Tests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IIncrementalGenerator generator = new MBW.Generators.OverloadGenerator.OverloadGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();
        var result = runResult.Results.Single();

        var sources = result.GeneratedSources.ToDictionary(x => x.HintName, x => x.SourceText.ToString());
        var diags = result.Diagnostics;
        return (sources, diags);
    }
}
