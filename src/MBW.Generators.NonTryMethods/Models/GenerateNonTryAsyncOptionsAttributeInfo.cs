using MBW.Generators.NonTryMethods.Attributes;

namespace MBW.Generators.NonTryMethods.Models;

public sealed class GenerateNonTryAsyncOptionsAttributeInfo(
    AsyncCandidateStrategy candidateStrategy = AsyncCandidateStrategy.TupleBooleanAndValue,
    AsyncGenerationStrategy generationStrategy = AsyncGenerationStrategy.Verbatim)
{
    public AsyncCandidateStrategy CandidateStrategy { get; } = candidateStrategy;
    public AsyncGenerationStrategy GenerationStrategy { get; } = generationStrategy;
}