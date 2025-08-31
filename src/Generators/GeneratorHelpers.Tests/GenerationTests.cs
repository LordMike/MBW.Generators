using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MBW.Generators.GeneratorHelpers.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MBW.Generators.GeneratorHelpers.Tests;

public class GenerationTests
{
    [Fact]
    public async Task TypeExactMatch_Unrolled()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
                                                               [GenerateSymbolExtensions]
                                                               public static class Known
                                                               {
                                                                   [SymbolNameExtension(MethodName="Test")]
                                                                   public const string Target = "MBW.Generators.OverloadGenerator.Attributes.TransformOverloadAttribute";
                                                               }
                                                               """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task GlobalPrefix_Unrolled()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
                                                               [GenerateSymbolExtensions]
                                                               public static class Known
                                                               {
                                                                   [SymbolNameExtension(MethodName="Exception")]
                                                                   public const string Target = "global::System.Exception";
                                                               }
                                                               """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task NestedType_Unrolled()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
                                                               [GenerateSymbolExtensions]
                                                               public static class Known
                                                               {
                                                                   [SymbolNameExtension(MethodName="Nested")]
                                                                   public const string Target = "System.IO.Compression.ZipArchive+Entry";
                                                               }
                                                               """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task NestedTypeInNestedType_Unrolled()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
                                                               [GenerateSymbolExtensions]
                                                               public static class Known
                                                               {
                                                                   [SymbolNameExtension(MethodName="Deep")]
                                                                   public const string Target = "System.Outer+Inner+Deep";
                                                               }
                                                               """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task GenericType_UsesMetadataName()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
                                                               [GenerateSymbolExtensions]
                                                               public static class Known
                                                               {
                                                                   [SymbolNameExtension(MethodName="Dict")]
                                                                   public const string Target = "System.Collections.Generic.Dictionary`2";
                                                               }
                                                               """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task NamespaceMethods_Unrolled()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
                                                               [GenerateSymbolExtensions]
                                                               public static class Known
                                                               {
                                                                   [NamespaceNameExtension(MethodName="SystemCollections")]
                                                                   public const string Target = "System.Collections";
                                                               }
                                                               """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task VisibilityMatches()
    {
        var (outputPub, diagsPub) = await TestsHelper.RunHelperAsync("""
                                                                     [GenerateSymbolExtensions]
                                                                     public class KnownPublic
                                                                     {
                                                                         [NamespaceNameExtension]
                                                                         public const string Ns = "System";
                                                                     }
                                                                     """);
        await VerifyHelper.VerifyGeneratorAsync(outputPub, diagsPub, name: "public");

        var (outputInt, diagsInt) = await TestsHelper.RunHelperAsync("""
                                                                     [GenerateSymbolExtensions]
                                                                     internal class KnownInternal
                                                                     {
                                                                         [NamespaceNameExtension]
                                                                         public const string Ns = "System";
                                                                     }
                                                                     """);
        await VerifyHelper.VerifyGeneratorAsync(outputInt, diagsInt, name: "internal");
    }

    [Fact]
    public async Task RuntimeCorrectness()
    {
        var source = """
                     using Microsoft.CodeAnalysis;

                     [GenerateSymbolExtensions]
                     public static class Known
                     {
                         [SymbolNameExtension]
                         public const string ExceptionType = "System.Exception";
                         [NamespaceNameExtension]
                         public const string SystemNs = "System";
                     }
                     """;

        var (output, diags) = await TestsHelper.RunHelperAsync(source);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);

        var harness = """
                      using System;
                      using System.Linq;
                      using Microsoft.CodeAnalysis;
                      using Microsoft.CodeAnalysis.CSharp;

                      public static class RuntimeHarness
                      {
                          public static bool[] Run()
                          {
                              var refs = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                                  .Select(a => MetadataReference.CreateFromFile(a.Location));
                              
                              var compilation = CSharpCompilation.Create("X", references: refs);
                              var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
                              var intType = compilation.GetTypeByMetadataName("System.Int32");
                              var ns = exceptionType!.ContainingNamespace;
                              var global = ns.ContainingNamespace;
                              
                              return new bool[]{
                                  exceptionType!.IsNamedExactlyTypeExceptionType(),
                                  intType!.IsNamedExactlyTypeExceptionType(),
                                  exceptionType!.IsInNamespaceSystemNs(),
                                  global.IsInNamespaceSystemNs(),
                                  ns.IsExactlyNamespaceSystemNs(),
                                  global.IsExactlyNamespaceSystemNs()};
                          }
                      }
                      """;

        SyntaxTree globalUsings = CSharpSyntaxTree.ParseText(
            "global using MBW.Generators.GeneratorHelpers.Attributes;",
            new CSharpParseOptions(LanguageVersion.Latest));

        var syntaxTrees = new[]
        {
            globalUsings,
            CSharpSyntaxTree.ParseText(source),
            CSharpSyntaxTree.ParseText(output!),
            CSharpSyntaxTree.ParseText(harness)
        };

        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));

        var compilation = CSharpCompilation.Create("Runtime", syntaxTrees, refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assert.Empty(compilation.GetDiagnostics()
            .Where(s => s.Severity == DiagnosticSeverity.Error));

        using var ms = new MemoryStream();
        using var msPdb = new MemoryStream();
        var result = compilation.Emit(ms, msPdb);
        Assert.True(result.Success);

        var asm = Assembly.Load(ms.ToArray(), msPdb.ToArray());
        var run = asm.GetType("RuntimeHarness")!.GetMethod("Run")!;
        var arr = (bool[])run.Invoke(null, null)!;

        Assert.True(arr[0]);
        Assert.False(arr[1]);
        Assert.True(arr[2]);
        Assert.False(arr[3]);
        Assert.True(arr[4]);
        Assert.False(arr[5]);
    }
}