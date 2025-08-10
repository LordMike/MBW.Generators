using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor SignatureCollision = new("OG001", "Generated signature collision", "Method '{0}' has a colliding overload", "OverloadGenerator", DiagnosticSeverity.Warning, true);
    public static readonly DiagnosticDescriptor MissingParameter = new("OG002", "Missing parameter", "Parameter '{1}' not found on method '{0}'", "OverloadGenerator", DiagnosticSeverity.Warning, true);
    public static readonly DiagnosticDescriptor InvalidAcceptType = new("OG003", "Accept type invalid", "Accept type for parameter '{0}' could not be resolved", "OverloadGenerator", DiagnosticSeverity.Warning, true);
    public static readonly DiagnosticDescriptor MissingValueToken = new("OG004", "Missing {value}", "Transform for parameter '{0}' must contain '{value}' token", "OverloadGenerator", DiagnosticSeverity.Warning, true);
    public static readonly DiagnosticDescriptor MissingDefaultExpression = new("OG005", "Default expression missing", "Default expression for parameter '{0}' is empty", "OverloadGenerator", DiagnosticSeverity.Warning, true);
}