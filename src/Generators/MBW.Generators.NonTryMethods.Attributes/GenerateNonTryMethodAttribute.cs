using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace MBW.Generators.NonTryMethods.Attributes;

[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    AllowMultiple = true, Inherited = false)]
[SuppressMessage("ReSharper", "RedundantNameQualifier")]
[Conditional("NEVER_RENDER")]
public sealed class GenerateNonTryMethodAttribute(
    Type? exceptionType = null,
    [RegexPattern] string? methodNamePattern = "^[Tt]ry(.*)")
    : Attribute
{
    /// <summary>
    /// The type of exception to throw when a Try-method returns false
    /// </summary>
    public Type? ExceptionType { get; } = exceptionType;

    /// <summary>
    /// The regex to find methods that should be converted to non-try methods
    /// </summary>
    public string? MethodNamePattern { get; } = methodNamePattern;
}