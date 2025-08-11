using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor NonTry_TargetNotPartial = new(
        "NT001", "Target type is not partial",
        "Type '{0}' must be declared partial to add generated methods", "NonTry", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NonTry_InvalidNameRegex = new(
        "NT002", "Invalid name regex",
        "Attribute on '{0}' has invalid regex '{1}': {2}", "NonTry", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NonTry_InvalidNameReplacement = new(
        "NT003", "Invalid name replacement",
        "Replacement '{1}' for pattern '{0}' on '{2}' failed: {3}", "NonTry", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NonTry_InvalidExceptionType = new(
        "NT004", "Invalid exception type",
        "Exception type '{0}' must derive from System.Exception", "NonTry", DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor NonTry_NotEligibleSync = new(
        "NT005", "Method not eligible (sync)",
        "Method '{0}' must return bool and have exactly one 'out' parameter", "NonTry", DiagnosticSeverity.Info,
        true);

    public static readonly DiagnosticDescriptor NonTry_NotEligibleAsyncShape = new(
        "NT006", "Method not eligible (async shape)",
        "Method '{0}' matches name but not async candidate strategy '{1}' (expect Task<(bool,T)> or ValueTask<(bool,T)>)",
        "NonTry", DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor NonTry_EmptyGeneratedName = new(
        "NT007", "Empty generated name",
        "Method '{0}' becomes empty after applying pattern '{1}' with replacement '{2}'", "NonTry",
        DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor NonTry_SignatureCollision = new(
        "NT008", "Generated signature collision",
        "Method '{0}' would collide with an existing member in '{1}'", "NonTry", DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor NonTry_DuplicateSpec = new(
        "NT009", "Duplicate generated signature",
        "Multiple attributes produce the same generated signature '{0}' in '{1}'; emitting once", "NonTry",
        DiagnosticSeverity.Warning, true);
}