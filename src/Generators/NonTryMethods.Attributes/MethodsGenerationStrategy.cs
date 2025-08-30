using System.Diagnostics.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Attributes;

/// <summary>
/// Specifies where generated non-<c>Try</c> methods should be placed.
/// </summary>
[PublicAPI]
[SuppressMessage("ReSharper", "RedundantNameQualifier")]
public enum MethodsGenerationStrategy
{
    /// <summary>
    /// Automatically pick the most suitable strategy. By default the
    /// generator emits methods into partial types when available.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Emit methods directly into the original type. This requires that
    /// the target type is declared <c>partial</c>.
    /// </summary>
    PartialType = 1,

    /// <summary>
    /// Emit methods as extension methods in a separate static class.
    /// </summary>
    Extensions = 2,
}
