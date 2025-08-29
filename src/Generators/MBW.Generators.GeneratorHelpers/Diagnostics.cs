using Microsoft.CodeAnalysis;

namespace MBW.Generators.GeneratorHelpers;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor FieldNotEligible = new(
        id: "GH0003",
        title: "Field is not eligible for extensions",
        messageFormat: "Field '{0}' must be a non-empty const string to be used for symbol extensions",
        category: "GeneratorHelpers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
