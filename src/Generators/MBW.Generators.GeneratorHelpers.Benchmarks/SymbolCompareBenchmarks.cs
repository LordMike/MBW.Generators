using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.GeneratorHelpers.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class SymbolCompareBenchmarks
{
    public Compilation _comp = default!;
    public INamedTypeSymbol _symbol = default!;

    // Use your actual target type
    public const string FullyQualified =
        "global::MBW.Generators.OverloadGenerator.Attributes.TransformOverloadAttribute";

    [GlobalSetup]
    public void Setup()
    {
        var code = SourceFactory.MakeYourExample();
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest));

        _comp = CSharpCompilation.Create(
            "BenchAsm",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var model = _comp.GetSemanticModel(tree, ignoreAccessibility: true);
        var root = tree.GetRoot();
        var cls = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TransformOverloadAttribute"); // our type
        _symbol = model.GetDeclaredSymbol(cls)!;
    }

    [Benchmark(Baseline = true)]
    public bool ToDisplayString_FullyQualified_Equals()
    {
        var s = _symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return string.Equals(s, FullyQualified, StringComparison.Ordinal);
    }

    [Benchmark]
    public bool Manual_MetadataName_And_Namespace_Equals()
        => ManualComparer.IsNamedExactlyFullyQualified(_symbol, FullyQualified);

    [Benchmark]
    public bool Manual_MetadataName_And_Namespace_Equals_Unrolled()
        => ManualComparer.IsNamedExactlyFullyQualified_Generated(_symbol);
}