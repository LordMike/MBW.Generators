using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Attributes;

/// <summary>
/// Marks an assembly or type for generation of non-<c>Try</c> methods.
/// </summary>
/// <remarks>
/// The generator scans the annotated scope for methods following the
/// configured <paramref name="methodNamePattern" /> (defaults to
/// <c>TryXyz</c>). For every match a new method is emitted that throws
/// <paramref name="exceptionType" /> when the original <c>Try</c> method
/// returns <c>false</c>.
/// </remarks>
/// <example>
/// Applying the attribute on a type:
/// <code>
/// [GenerateNonTryMethod(typeof(InvalidOperationException))]
/// public partial class Parser
/// {
///     public bool TryParse(string input, out int value) => int.TryParse(input, out value);
/// }
/// // Generates:
/// // public int Parse(string input)
/// // {
/// //     if (TryParse(input, out var value))
/// //         return value;
/// //     throw new InvalidOperationException();
/// // }
/// </code>
/// The attribute can also be applied at the assembly level to affect
/// multiple types.
/// </example>
[PublicAPI]
[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    AllowMultiple = true, Inherited = false)]
[Conditional("NEVER_RENDER")]
[SuppressMessage("ReSharper", "RedundantNameQualifier")]
public sealed class GenerateNonTryMethodAttribute(
    Type? exceptionType = null,
    [RegexPattern] string? methodNamePattern = "^[Tt]ry(.*)")
    : Attribute
{
    /// <summary>
    /// The type of exception to throw when a <c>Try</c>-method returns <c>false</c>.
    /// </summary>
    public Type? ExceptionType { get; } = exceptionType;

    /// <summary>
    /// Regex used to locate methods that should receive non-<c>Try</c> counterparts.
    /// Defaults to <c>^[Tt]ry(.*)</c>.
    /// </summary>
    public string? MethodNamePattern { get; } = methodNamePattern;
}
