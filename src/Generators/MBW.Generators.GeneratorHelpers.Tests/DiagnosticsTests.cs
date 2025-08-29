using System.Threading.Tasks;
using MBW.Generators.GeneratorHelpers;
using MBW.Generators.GeneratorHelpers.Tests.Helpers;
using Xunit;

namespace MBW.Generators.GeneratorHelpers.Tests;

public class DiagnosticsTests
{
    [Fact]
    public async Task FieldNotConstProducesDiagnostic()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            internal static class Known
            {
                [SymbolNameExtension]
                public static readonly string ExceptionType = "global::System.Exception";
            }
            """);

        Assert.Null(output);
        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.FieldNotEligible.Id, d.Id));
    }

    [Fact]
    public async Task FieldNotStringProducesDiagnostic()
    {
        var (output, diags) = await TestsHelper.RunHelperAsync("""
            [GenerateSymbolExtensions]
            internal static class Known
            {
                [SymbolNameExtension]
                public const int ExceptionType = 1;
            }
            """);

        Assert.Null(output);
        Assert.Collection(diags,
            d => Assert.Equal(Diagnostics.FieldNotEligible.Id, d.Id));
    }
}
