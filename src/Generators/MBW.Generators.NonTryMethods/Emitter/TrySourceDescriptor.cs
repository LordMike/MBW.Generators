using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Emitter;

internal readonly record struct TrySourceDescriptor(
    string Name,
    bool IsStatic,
    INamedTypeSymbol? ContainingType,
    TryShape Shape,
    int? OutParamIndex,
    bool UnwrapNullableValue,
    ImmutableArray<ITypeSymbol> TypeArguments
);