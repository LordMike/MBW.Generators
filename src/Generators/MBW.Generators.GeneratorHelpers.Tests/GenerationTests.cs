using System.Threading.Tasks;
using MBW.Generators.GeneratorHelpers.Tests.Helpers;
using Xunit;

namespace MBW.Generators.GeneratorHelpers.Tests;

public class GenerationTests
{
    [Fact]
    public async Task StaticClassGeneratesExtensions()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            internal static class Known
            {
                [SymbolNameExtension]
                public const string ExceptionType = "global::System.Exception";
            }
            """);

        Assert.NotNull(output);
        Assert.Empty(diags);
        Assert.Contains("IsNamedExactlyTypeExceptionType", output);
    }

    [Fact]
    public async Task NonStaticClassGeneratesExtensions()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            internal class Known
            {
                [SymbolNameExtension]
                public const string ExceptionType = "global::System.Exception";
            }
            """);

        Assert.NotNull(output);
        Assert.Empty(diags);
        Assert.Contains("IsNamedExactlyTypeExceptionType", output);
    }

    [Fact]
    public async Task StructGeneratesExtensions()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            internal struct Known
            {
                [SymbolNameExtension]
                public const string ExceptionType = "global::System.Exception";
            }
            """);

        Assert.NotNull(output);
        Assert.Empty(diags);
        Assert.Contains("IsNamedExactlyTypeExceptionType", output);
    }
}
