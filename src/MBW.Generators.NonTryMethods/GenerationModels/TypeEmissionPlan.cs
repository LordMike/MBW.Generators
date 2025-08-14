using MBW.Generators.NonTryMethods.Attributes;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.GenerationModels;

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