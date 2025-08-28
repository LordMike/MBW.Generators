using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Emitter;

internal readonly record struct NonTryModel(
    string GeneratedName,
    ITypeSymbol GeneratedReturnType,
    ImmutableArray<ParameterModel> Parameters,
    TrySourceDescriptor Source,
    INamedTypeSymbol? ExceptionType,
    ImmutableArray<ITypeParameterSymbol> MethodTypeParams,
    ImmutableArray<ITypeParameterSymbol> LiftedReceiverTypeParams
);