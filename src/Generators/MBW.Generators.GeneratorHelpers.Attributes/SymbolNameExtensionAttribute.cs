using System;

namespace MBW.Generators.GeneratorHelpers;

/// <summary>Generates extension methods for a fully qualified type name.</summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SymbolNameExtensionAttribute : Attribute
{
    /// <summary>Override for method suffix N. Default = field name.</summary>
    public string? MethodName { get; set; }
}
