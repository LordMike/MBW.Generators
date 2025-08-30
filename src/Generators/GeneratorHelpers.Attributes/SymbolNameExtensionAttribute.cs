using System;

namespace MBW.Generators.GeneratorHelpers.Attributes;

/// <summary>
/// Identifies a <c>const string</c> field whose value is a fully qualified type name.
/// The generator emits an extension method that checks for this exact type.
/// </summary>
/// <remarks>
/// The containing type must also be annotated with <see cref="GenerateSymbolExtensionsAttribute"/> for this attribute to
/// have an effect.
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SymbolNameExtensionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the suffix used for the generated method name.
    /// </summary>
    /// <value>If omitted, the field name is used.</value>
    public string? MethodName { get; set; }
}
