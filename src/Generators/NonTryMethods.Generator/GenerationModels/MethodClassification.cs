using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Generator.GenerationModels;

internal readonly struct MethodClassification
{
    public readonly MethodShape Shape;
    public readonly IParameterSymbol? OutParam; // for SyncBoolOut
    public readonly ITypeSymbol? PayloadType; // for AsyncTuple (T in (bool, T))
    public readonly INamedTypeSymbol? WrapperType; // Task<T> or ValueTask<T>

    public MethodClassification(
        MethodShape shape,
        IParameterSymbol? outParam,
        ITypeSymbol? payloadType,
        INamedTypeSymbol? wrapperType)
    {
        Shape = shape;
        OutParam = outParam;
        PayloadType = payloadType;
        WrapperType = wrapperType;
    }
}