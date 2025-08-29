using System;

namespace MBW.Generators.GeneratorHelpers;

/// <summary>Marks a type for symbol extension generation.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class GenerateSymbolExtensionsAttribute : Attribute
{
    /// <summary>Optional override for the generated extensions class name. Default: &lt;TypeName&gt;Extensions.</summary>
    public string? Name { get; set; }

    /// <summary>Optional override for the namespace of the generated extensions class. Default: "Microsoft.CodeAnalysis".</summary>
    public string? Namespace { get; set; }
}
