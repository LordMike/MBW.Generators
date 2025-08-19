using Microsoft.CodeAnalysis;

namespace MBW.Generators.Common;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor ExceptionError = new(
        id: "MB001",
        title: "An internal error occurred in the generator",
        messageFormat:
            "The {0} Generator encountered an issue when generating, exception: {1}, message: {2}, Stack: {3}",
        category: "Internal",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
