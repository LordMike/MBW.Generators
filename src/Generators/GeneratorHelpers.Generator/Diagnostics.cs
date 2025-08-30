using Microsoft.CodeAnalysis;

namespace MBW.Generators.GeneratorHelpers.Generator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor TypeMissingFields = new(
        id: "GH0001",
        title: "Type has no symbol extension fields",
        messageFormat:
        "Type '{0}' has [GenerateSymbolExtensions] but no fields with [SymbolNameExtension] or [NamespaceNameExtension]",
        category: "GeneratorHelpers",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FieldWithoutOptIn = new(
        id: "GH0002",
        title: "Field requires [GenerateSymbolExtensions]",
        messageFormat: "Field '{0}' has a symbol extension attribute but its containing type lacks [GenerateSymbolExtensions]",
        category: "GeneratorHelpers",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidFieldTarget = new(
        id: "GH0003",
        title: "Field is not eligible for extensions",
        messageFormat: "Field '{0}' must be a non-empty const string to be used for symbol extensions",
        category: "GeneratorHelpers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidTypeFqn = new(
        id: "GH0004",
        title: "Invalid type fully-qualified name",
        messageFormat: "Field '{0}' has invalid type name '{1}'",
        category: "GeneratorHelpers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidNamespaceFqn = new(
        id: "GH0005",
        title: "Invalid namespace fully-qualified name",
        messageFormat: "Field '{0}' has invalid namespace name '{1}'",
        category: "GeneratorHelpers",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateTarget = new(
        id: "GH0006",
        title: "Duplicate target",
        messageFormat: "Field '{0}' references a duplicate target '{1}'",
        category: "GeneratorHelpers",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateMethodName = new(
        id: "GH0007",
        title: "Duplicate method name",
        messageFormat: "Field '{0}' would generate duplicate method name '{1}'",
        category: "GeneratorHelpers",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
