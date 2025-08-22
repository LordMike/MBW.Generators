using MBW.Generators.NonTryMethods.Attributes;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed class GenerateNonTryOptionsAttributeInfo(
    AsyncCandidateStrategy asyncCandidateStrategy = AsyncCandidateStrategy.TupleBooleanAndValue,
    ReturnGenerationStrategy returnGenerationStrategy = ReturnGenerationStrategy.Verbatim,
    MethodsGenerationStrategy methodsGenerationStrategy = MethodsGenerationStrategy.Auto)
{
    public AsyncCandidateStrategy AsyncCandidateStrategy { get; } = asyncCandidateStrategy;
    public ReturnGenerationStrategy ReturnGenerationStrategy { get; } = returnGenerationStrategy;
    public MethodsGenerationStrategy MethodsGenerationStrategy { get; } = methodsGenerationStrategy;
}