using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Attributes;

/// <summary>
/// Configures how <see cref="GenerateNonTryMethodAttribute" /> produces
/// non-<c>Try</c> methods.
/// </summary>
/// <remarks>
/// Apply this attribute to a type or assembly to override the default
/// generation strategy used for matching <c>Try</c> methods.
/// </remarks>
/// <example>
/// <code>
/// [assembly: GenerateNonTryOptions(
///     AsyncCandidateStrategy.TupleBooleanAndValue,
///     ReturnGenerationStrategy.TrueMeansNotNull,
///     MethodsGenerationStrategy.Extensions)]
/// </code>
/// This example instructs the generator to consider
/// <c>Task&lt;(bool Success, T Value)&gt;</c> methods, treat a <c>true</c>
/// return as implying a non-null value and emit the new methods as
/// extension methods.
/// </example>
[PublicAPI]
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
    /// <summary>
    /// Determines which asynchronous <c>Try</c> methods are eligible for
    /// non-<c>Try</c> generation.
    /// </summary>
    public AsyncCandidateStrategy AsyncCandidateStrategy { get; } = asyncCandidateStrategy;

    /// <summary>
    /// Controls how the return type of generated methods relates to the
    /// original <c>out</c> parameter.
    /// </summary>
    public ReturnGenerationStrategy ReturnGenerationStrategy { get; } = returnGenerationStrategy;

    /// <summary>
    /// Specifies where generated methods will be placed (partial types or
    /// extension classes).
    /// </summary>
    public MethodsGenerationStrategy MethodsGenerationStrategy { get; } = methodsGenerationStrategy;
}
