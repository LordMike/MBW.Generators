using System.Collections.Immutable;
using MBW.Generators.NonTryMethods.Attributes;
using MBW.Generators.NonTryMethods.Models;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.GenerationModels;

// Helper enums for planning
internal enum EmissionKind { Partial, InterfaceDefault, Extension }
internal enum MethodShape { SyncBoolOut, AsyncTuple, NotEligible }

// ----------------------------------------------------------------------------
// Readonly planning structs (pure data carriers)
// ----------------------------------------------------------------------------

internal readonly struct TypeEmissionPlan
{
    public readonly INamedTypeSymbol Type;
    public readonly MethodsGenerationStrategy Strategy;
    public readonly bool CanHostPartials;
    public readonly bool IsInterface;
    public readonly bool SupportsInterfaceDefaults;

    public TypeEmissionPlan(
        INamedTypeSymbol type,
        MethodsGenerationStrategy strategy,
        bool canHostPartials,
        bool isInterface,
        bool supportsInterfaceDefaults)
    {
        Type = type;
        Strategy = strategy;
        CanHostPartials = canHostPartials;
        IsInterface = isInterface;
        SupportsInterfaceDefaults = supportsInterfaceDefaults;
    }
}

internal readonly struct MethodClassification
{
    public readonly MethodShape Shape;
    public readonly IParameterSymbol? OutParam; // for SyncBoolOut
    public readonly bool IsValueTask;           // for AsyncTuple
    public readonly ITypeSymbol? PayloadType;   // for AsyncTuple (T in (bool, T))

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

internal readonly struct PlannedMethod
{
    public readonly MethodSpec Source;      // your existing MethodSpec
    public readonly PlannedSignature Signature;
    public readonly ITypeSymbol ExceptionType;
    public readonly bool IsAsync;
    public readonly bool IsValueTask;

    public PlannedMethod(
        MethodSpec source,
        PlannedSignature signature,
        ITypeSymbol exceptionType,
        bool isAsync,
        bool isValueTask)
    {
        Source = source;
        Signature = signature;
        ExceptionType = exceptionType;
        IsAsync = isAsync;
        IsValueTask = isValueTask;
    }
}
