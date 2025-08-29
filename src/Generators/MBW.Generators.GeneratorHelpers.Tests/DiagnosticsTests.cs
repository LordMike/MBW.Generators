using System.Threading.Tasks;
using MBW.Generators.GeneratorHelpers;
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

        Assert.Null(output);
        Assert.Collection(diags, d => Assert.Equal(Diagnostics.TypeMissingFields.Id, d.Id));
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

        Assert.Null(output);
        Assert.Collection(diags, d => Assert.Equal(Diagnostics.FieldWithoutOptIn.Id, d.Id));
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

        Assert.Null(output);
        Assert.Collection(diags, d => Assert.Equal(Diagnostics.InvalidFieldTarget.Id, d.Id));
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

        Assert.Null(output);
        Assert.Collection(diags, d => Assert.Equal(Diagnostics.InvalidTypeFqn.Id, d.Id));
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

        Assert.Null(output);
        Assert.Collection(diags, d => Assert.Equal(Diagnostics.InvalidNamespaceFqn.Id, d.Id));
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

        Assert.NotNull(output);
        Assert.Collection(diags, d => Assert.Equal(Diagnostics.DuplicateTarget.Id, d.Id));
        Assert.Contains("IsNamedExactlyTypeEx1", output);
        Assert.DoesNotContain("IsNamedExactlyTypeEx2", output);
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

        Assert.NotNull(output);
        Assert.Collection(diags, d => Assert.Equal(Diagnostics.DuplicateMethodName.Id, d.Id));
        Assert.Contains("IsNamedExactlyTypeDup(", output);
        Assert.Contains("IsNamedExactlyTypeDup_2(", output);
    }
}

