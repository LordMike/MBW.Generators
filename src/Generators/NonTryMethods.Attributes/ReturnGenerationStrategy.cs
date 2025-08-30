using System.Diagnostics.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Attributes;

/// <summary>
/// Controls how the return type of generated non-<c>Try</c> methods is
/// inferred from the original method's <c>out</c> parameter.
/// </summary>
[SuppressMessage("ReSharper", "RedundantNameQualifier")]
public enum ReturnGenerationStrategy
{
    /// <summary>
    /// Match the return type exactly to the <c>out</c> parameter, preserving
    /// any nullability annotations.
    /// </summary>
    Verbatim = 0,

    /// <summary>
    /// Assume that a <c>true</c> result from the <c>Try</c> method implies a
    /// non-null <c>out</c> value and remove nullable annotations from the
    /// generated return type.
    /// </summary>
    TrueMeansNotNull = 1,
}
