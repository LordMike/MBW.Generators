using System.Collections.Immutable;
using MBW.Generators.NonTryMethods.Emitter;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.GenerationModels;

internal readonly struct PlannedSignature
{
    public readonly EmissionKind Kind;
    public readonly string Name;
    public readonly ITypeSymbol ReturnType;
    public readonly ImmutableArray<IParameterSymbol> Parameters; // final parameters (includes "this T" for extensions)
    public readonly bool IsStatic;

    public PlannedSignature(
        EmissionKind kind,
        string name,
        ITypeSymbol returnType,
        ImmutableArray<IParameterSymbol> parameters,
        bool isStatic)
    {
        Kind = kind;
        Name = name;
        ReturnType = returnType;
        Parameters = parameters;
        IsStatic = isStatic;
    }
}