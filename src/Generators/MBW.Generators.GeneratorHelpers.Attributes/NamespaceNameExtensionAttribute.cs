using System;

namespace MBW.Generators.GeneratorHelpers;

/// <summary>Generates namespace comparison extensions for a fully qualified namespace.</summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class NamespaceNameExtensionAttribute : Attribute
{
    /// <summary>Override for method suffix N. Default = field name.</summary>
    public string? MethodName { get; set; }
}
