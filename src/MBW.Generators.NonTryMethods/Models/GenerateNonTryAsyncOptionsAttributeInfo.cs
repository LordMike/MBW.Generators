using MBW.Generators.NonTryMethods.Attributes;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed class GenerateNonTryOptionsAttributeInfo(
    Location location,
    AsyncCandidateStrategy asyncCandidateStrategy = AsyncCandidateStrategy.TupleBooleanAndValue,
    ReturnGenerationStrategy returnGenerationStrategy = ReturnGenerationStrategy.Verbatim,
    MethodsGenerationStrategy methodsGenerationStrategy = MethodsGenerationStrategy.Auto)
{
    public Location Location { get; } = location;
    public AsyncCandidateStrategy AsyncCandidateStrategy { get; } = asyncCandidateStrategy;
    public ReturnGenerationStrategy ReturnGenerationStrategy { get; } = returnGenerationStrategy;
    public MethodsGenerationStrategy MethodsGenerationStrategy { get; } = methodsGenerationStrategy;
}