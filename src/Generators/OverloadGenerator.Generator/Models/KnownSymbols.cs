using MBW.Generators.GeneratorHelpers;
using MBW.Generators.GeneratorHelpers.Attributes;

namespace MBW.Generators.OverloadGenerator.Generator.Models;

[GenerateSymbolExtensions]
internal static partial class KnownSymbols
{
    [SymbolNameExtension]
    public const string DefaultOverloadAttribute = "MBW.Generators.OverloadGenerator.Attributes.DefaultOverloadAttribute";

    [SymbolNameExtension]
    public const string TransformOverloadAttribute = "MBW.Generators.OverloadGenerator.Attributes.TransformOverloadAttribute";
}
