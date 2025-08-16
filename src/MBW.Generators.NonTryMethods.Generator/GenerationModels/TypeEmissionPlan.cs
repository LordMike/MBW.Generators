using MBW.Generators.NonTryMethods.Attributes;

namespace MBW.Generators.NonTryMethods.GenerationModels;

internal readonly struct TypeEmissionPlan(
    MethodsGenerationStrategy strategy,
    bool isInterface)
{
    public readonly MethodsGenerationStrategy Strategy = strategy;
    public readonly bool IsInterface = isInterface;
}