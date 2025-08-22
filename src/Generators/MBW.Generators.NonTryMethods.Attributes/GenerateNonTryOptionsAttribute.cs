using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Attributes;

[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    Inherited = false)]
[SuppressMessage("ReSharper", "RedundantNameQualifier")]
[Conditional("NEVER_RENDER")]
public sealed class GenerateNonTryOptionsAttribute(
    AsyncCandidateStrategy asyncCandidateStrategy = AsyncCandidateStrategy.TupleBooleanAndValue,
    ReturnGenerationStrategy returnGenerationStrategy = ReturnGenerationStrategy.Verbatim,
    MethodsGenerationStrategy methodsGenerationStrategy = MethodsGenerationStrategy.Auto) : Attribute
{
    public AsyncCandidateStrategy AsyncCandidateStrategy { get; } = asyncCandidateStrategy;
    public ReturnGenerationStrategy ReturnGenerationStrategy { get; } = returnGenerationStrategy;
    public MethodsGenerationStrategy MethodsGenerationStrategy { get; } = methodsGenerationStrategy;
}