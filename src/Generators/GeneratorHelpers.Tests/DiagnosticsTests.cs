using System.Threading.Tasks;
using MBW.Generators.GeneratorHelpers.Tests.Helpers;
using Xunit;

namespace MBW.Generators.GeneratorHelpers.Tests;

public class DiagnosticsTests
{
    [Fact]
    public async Task GH0001_TypeMissingFields()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            public class Known { }
            """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task GH0002_FieldWithoutOptIn()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            public class Known
            {
                [SymbolNameExtension]
                public const string Ex = "System.Exception";
            }
            """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task GH0003_InvalidFieldTarget()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            public class Known
            {
                [SymbolNameExtension]
                public static readonly string Ex = "System.Exception";
            }
            """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task GH0004_InvalidTypeFqn()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            public class Known
            {
                [SymbolNameExtension]
                public const string Ex = "Foo";
            }
            """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task GH0005_InvalidNamespaceFqn()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            public class Known
            {
                [NamespaceNameExtension]
                public const string Ns = "System..";
            }
            """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task GH0006_DuplicateTarget()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            public class Known
            {
                [SymbolNameExtension]
                public const string Ex1 = "System.Exception";
                [SymbolNameExtension]
                public const string Ex2 = "global::System.Exception";
            }
            """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task GH0007_DuplicateMethodName()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            public class Known
            {
                [SymbolNameExtension(MethodName="Dup")]
                public const string Ex1 = "System.Exception";
                [SymbolNameExtension(MethodName="Dup")]
                public const string Ex2 = "System.ArgumentException";
            }
            """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}

