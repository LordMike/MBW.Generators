using MBW.Generators.NonTryMethods.Generator.Models;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Generator.GenerationModels;

internal readonly struct PlannedMethod
{
    public readonly MethodSpec Source; // your existing MethodSpec
    public readonly PlannedSignature Signature;
    public readonly INamedTypeSymbol ExceptionType;
    public readonly bool IsAsync;
    public readonly bool UnwrapNullable;

    public PlannedMethod(
        MethodSpec source,
        PlannedSignature signature,
        INamedTypeSymbol exceptionType,
        bool isAsync,
        bool unwrapNullable)
    {
        Source = source;
        Signature = signature;
        ExceptionType = exceptionType;
        IsAsync = isAsync;
        UnwrapNullable = unwrapNullable;
    }
}