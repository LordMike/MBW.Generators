using System;

namespace MBW.Generators.NonTryMethods.Attributes;

[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
    AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class GenerateNonTryAsyncOptionsAttribute(
    AsyncCandidateStrategy candidateStrategy = AsyncCandidateStrategy.TupleBooleanAndValue,
    AsyncGenerationStrategy generationStrategy = AsyncGenerationStrategy.Verbatim) : Attribute
{
    public AsyncCandidateStrategy CandidateStrategy { get; } = candidateStrategy;
    public AsyncGenerationStrategy GenerationStrategy { get; } = generationStrategy;
}