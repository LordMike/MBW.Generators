using MBW.Generators.GeneratorHelpers;
using MBW.Generators.GeneratorHelpers.Attributes;

namespace MBW.Generators.GeneratorHelpers.Generator.Models;

[GenerateSymbolExtensions]
internal static partial class KnownSymbols
{
    [SymbolNameExtension]
    public const string GenerateSymbolExtensionsAttribute =
        "MBW.Generators.GeneratorHelpers.Attributes.GenerateSymbolExtensionsAttribute";

    [SymbolNameExtension]
    public const string SymbolNameExtensionAttribute =
        "MBW.Generators.GeneratorHelpers.Attributes.SymbolNameExtensionAttribute";

    [SymbolNameExtension]
    public const string NamespaceNameExtensionAttribute =
        "MBW.Generators.GeneratorHelpers.Attributes.NamespaceNameExtensionAttribute";
}