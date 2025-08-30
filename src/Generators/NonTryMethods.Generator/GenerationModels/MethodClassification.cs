using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Generator.GenerationModels;

internal readonly struct MethodClassification
{
    public readonly MethodShape Shape;
    public readonly IParameterSymbol? OutParam; // for SyncBoolOut
    public readonly bool IsValueTask; // for AsyncTuple
    public readonly ITypeSymbol? PayloadType; // for AsyncTuple (T in (bool, T))

    public MethodClassification(
        MethodShape shape,
        IParameterSymbol? outParam,
        bool isValueTask,
        ITypeSymbol? payloadType)
    {
        Shape = shape;
        OutParam = outParam;
        IsValueTask = isValueTask;
        PayloadType = payloadType;
    }
}