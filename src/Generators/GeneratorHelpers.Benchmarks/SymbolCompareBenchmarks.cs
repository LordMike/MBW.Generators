using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.GeneratorHelpers.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class SymbolCompareBenchmarks
{
    public Compilation Comp = default!;
    public INamedTypeSymbol Symbol = default!;

    // Use your actual target type
    public const string FullyQualified =
        "global::MBW.Generators.OverloadGenerator.Attributes.TransformOverloadAttribute";

    [GlobalSetup]
    public void Setup()
    {
        var code = SourceFactory.MakeYourExample();
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest));

        Comp = CSharpCompilation.Create(
            "BenchAsm",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var model = Comp.GetSemanticModel(tree, ignoreAccessibility: true);
        var root = tree.GetRoot();
        var cls = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TransformOverloadAttribute"); // our type
        Symbol = model.GetDeclaredSymbol(cls)!;
    }

    [Benchmark(Baseline = true)]
    public bool ToDisplayString_FullyQualified_Equals()
    {
        var s = Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return string.Equals(s, FullyQualified, StringComparison.Ordinal);
    }

    [Benchmark]
    public bool IsNamedExactlyType_SpanVersion()
        => ManualComparer.IsNamedExactlyType_SpanVersion(Symbol, FullyQualified);

    [Benchmark]
    public bool IsNamedExactlyType_GeneratedByGenerator()
        => ManualComparer.IsNamedExactlyType_GeneratedByGenerator(Symbol);
}