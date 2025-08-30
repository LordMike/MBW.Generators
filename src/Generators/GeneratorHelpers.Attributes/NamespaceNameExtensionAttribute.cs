using System;
using System.Diagnostics;

namespace MBW.Generators.GeneratorHelpers.Attributes;

/// <summary>
/// Identifies a <c>const string</c> field whose value is a fully qualified namespace.
/// The generator emits extensions that test whether symbols reside in or exactly match this namespace.
/// </summary>
/// <remarks>
/// The containing type must also be annotated with <see cref="GenerateSymbolExtensionsAttribute"/> for this attribute to
/// have an effect.
/// </remarks>
[Conditional("NEVER_RENDER")]
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class NamespaceNameExtensionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the suffix used for generated method names.
    /// </summary>
    /// <value>If omitted, the field name is used.</value>
    public string? MethodName { get; set; }
}
