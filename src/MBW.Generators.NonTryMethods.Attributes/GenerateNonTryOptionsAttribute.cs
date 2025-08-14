using System;

namespace MBW.Generators.NonTryMethods.Attributes;

[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    Inherited = false)]
public sealed class GenerateNonTryOptionsAttribute(
    AsyncCandidateStrategy asyncCandidateStrategy = AsyncCandidateStrategy.TupleBooleanAndValue,
    ReturnGenerationStrategy returnGenerationStrategy = ReturnGenerationStrategy.Verbatim,
    MethodsGenerationStrategy methodsGenerationStrategy = MethodsGenerationStrategy.Auto) : Attribute
{
    public AsyncCandidateStrategy AsyncCandidateStrategy { get; } = asyncCandidateStrategy;
    public ReturnGenerationStrategy ReturnGenerationStrategy { get; } = returnGenerationStrategy;
    public MethodsGenerationStrategy MethodsGenerationStrategy { get; } = methodsGenerationStrategy;
}