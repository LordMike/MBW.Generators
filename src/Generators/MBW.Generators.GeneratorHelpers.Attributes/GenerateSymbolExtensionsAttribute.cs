using System;

namespace MBW.Generators.GeneratorHelpers.Attributes;

/// <summary>
/// Marks a type whose <c>const string</c> fields describe symbols for which extension methods are generated.
/// </summary>
/// <remarks>
/// Applicable fields must be annotated with either <see cref="SymbolNameExtensionAttribute"/> or
/// <see cref="NamespaceNameExtensionAttribute"/>. Both this attribute and one of the field attributes are required for
/// generation to occur.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class GenerateSymbolExtensionsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an optional name for the generated extensions class.
    /// </summary>
    /// <value>Defaults to <c>&lt;TypeName&gt;Extensions</c> when not specified.</value>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets an optional namespace for the generated extensions class.
    /// </summary>
    /// <value>Defaults to <c>Microsoft.CodeAnalysis</c> when not specified.</value>
    public string? Namespace { get; set; }
}
