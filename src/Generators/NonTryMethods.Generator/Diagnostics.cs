using MBW.Generators.NonTryMethods.Attributes;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Generator;

internal static class Diagnostics
{
    /// <summary>
    /// The chosen MethodsGenerationStrategy cannot be applied to the target type
    /// (e.g., PartialType requires 'partial', Extensions not supported/invalid).
    /// The user likely needs to tweak <see cref="GenerateNonTryOptionsAttribute"/>.
    /// </summary>
    public static readonly DiagnosticDescriptor StrategyRequirementsNotMet = new(
        id: "NT001",
        title: "Generation strategy requirements not met",
        messageFormat:
            "MethodsGenerationStrategy '{0}' from [GenerateNonTryOptions] cannot be applied to target type '{1}': {2}",
        category: "NonTry",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Custom exception type must derive from System.Exception.
    /// This is configured via <see cref="GenerateNonTryMethodAttribute"/>.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidExceptionType = new(
        id: "NT002",
        title: "Invalid exception type",
        messageFormat:
            "Exception type '{0}' specified via [GenerateNonTryMethod] must derive from System.Exception",
        category: "NonTry",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Method matched a non-try pattern from <see cref="GenerateNonTryMethodAttribute"/> but is not a valid sync Try:
    /// must return bool and have exactly one 'out' parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor NotEligibleSync = new(
        id: "NT003",
        title: "Method not eligible (sync shape)",
        messageFormat:
            "Method '{0}' matched [GenerateNonTryMethod] pattern '{1}' but is not eligible: must return 'bool' and have exactly one 'out' parameter",
        category: "NonTry",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// Method matched a non-try pattern from <see cref="GenerateNonTryMethodAttribute"/> but doesn't match the async candidate strategy
    /// configured by <see cref="GenerateNonTryOptionsAttribute"/>.
    /// </summary>
    public static readonly DiagnosticDescriptor NotEligibleAsyncShape = new(
        id: "NT004",
        title: "Method not eligible (async shape)",
        messageFormat:
            "Method '{0}' matched [GenerateNonTryMethod] pattern '{1}' but is excluded by async candidate strategy '{2}' from [GenerateNonTryOptions] (expected Task<(bool,T)> or ValueTask<(bool,T)>)",
        category: "NonTry",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// Generated signature would collide with an existing member in the target type.
    /// The colliding output originates from <see cref="GenerateNonTryMethodAttribute"/>.
    /// </summary>
    public static readonly DiagnosticDescriptor SignatureCollision = new(
        id: "NT005",
        title: "Generated signature collision",
        messageFormat:
            "Generated method '{0}' from [GenerateNonTryMethod] would collide with an existing member in '{1}'; skipping",
        category: "NonTry",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// Multiple GenerateNonTryMethodAttribute patterns produce the same generated signature; emit once.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateGeneratedSignature = new(
        id: "NT006",
        title: "Duplicate generated signature",
        messageFormat:
        "Multiple [GenerateNonTryMethod]s produce the same generated signature '{0}' in '{1}'; emitting once",
        category: "NonTry",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor MultiplePatternsMatchMethod = new(
        id: "NT007",
        title: "Multiple patterns match method",
        messageFormat:
        "Multiple [GenerateNonTryMethod] patterns match '{0}' in '{1}' and result in the same generated signature, The patterns were: {2}; emitting once",
        category: "NonTry",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnableToGenerateExtensionMethodForStaticMethod = new(
        id: "NT008",
        title: "Cannot generate extension for static method",
        messageFormat:
        "Method '{0}' in '{1}' is static; cannot generate an extension method that requires an instance receiver; skipping",
        category: "NonTry",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RegularExpressionIsInvalid = new(
        id: "NT009",
        title: "An invalid regular expression was supplied to a [GenerateNonTryMethod]",
        messageFormat:
        "The regular expression '{0}' is invalid, it should be valid and have have exactly one capture group which indicates what the non-try method name will be",
        category: "NonTry",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
