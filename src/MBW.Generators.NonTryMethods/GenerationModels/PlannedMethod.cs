using MBW.Generators.NonTryMethods.Models;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.GenerationModels;

internal readonly struct PlannedMethod
{
    public readonly MethodSpec Source; // your existing MethodSpec
    public readonly PlannedSignature Signature;
    public readonly INamedTypeSymbol ExceptionType;
    public readonly bool IsAsync;

    public PlannedMethod(
        MethodSpec source,
        PlannedSignature signature,
        INamedTypeSymbol exceptionType,
        bool isAsync)
    {
        Source = source;
        Signature = signature;
        ExceptionType = exceptionType;
        IsAsync = isAsync;
    }
}