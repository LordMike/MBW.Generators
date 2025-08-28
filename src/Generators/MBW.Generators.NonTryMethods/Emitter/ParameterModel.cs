using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Emitter;

internal readonly record struct ParameterModel(
    string Name,
    ITypeSymbol Type,
    RefKind RefKind,
    bool IsParams = false,
    bool HasDefault = false,
    object? DefaultValue = null);