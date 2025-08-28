// NonTryEmitter.cs
// Internal, no-Workspaces, single-file NonTry emitter.
// Integrates GenerationHelpers.ToCSharpLiteralExpression and FindUnusedParamName.

using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Emitter;

// Lean models
internal readonly record struct EmissionPlan(
    EmissionKind Kind,
    string? SelfName, // null/empty => compute via FindUnusedParamName("self")
    INamedTypeSymbol? ExtensionReceiverType,
    RefKind ReceiverRefKind
);