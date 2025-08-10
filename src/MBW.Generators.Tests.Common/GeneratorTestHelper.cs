using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MBW.Generators.Tests.Common;

public static class GeneratorTestHelper
{
    public static (IReadOnlyDictionary<string, string> Sources, IReadOnlyList<Diagnostic> Diagnostics) Run<TGenerator>(string source, params Type[] referencedTypes)
        where TGenerator : IIncrementalGenerator, new()
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

        List<MetadataReference> references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location)
        };

        foreach (Type type in referencedTypes)
            references.Add(MetadataReference.CreateFromFile(type.Assembly.Location));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Tests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IIncrementalGenerator generator = new TGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();

        Dictionary<string, string> sources = result.GeneratedSources.ToDictionary(x => x.HintName, x => x.SourceText.ToString());
        ImmutableArray<Diagnostic> diags = result.Diagnostics;
        return (sources, diags);
    }
}
