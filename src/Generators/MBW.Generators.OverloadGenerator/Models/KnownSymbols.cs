using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Models;

internal sealed class KnownSymbols
{
    public const string DefaultOverloadAttributeName =
        "MBW.Generators.OverloadGenerator.Attributes.DefaultOverloadAttribute";

    public const string TransformOverloadAttributeName =
        "MBW.Generators.OverloadGenerator.Attributes.TransformOverloadAttribute";

    public readonly INamedTypeSymbol DefaultOverloadAttribute;
    public readonly INamedTypeSymbol TransformOverloadAttribute;

    private KnownSymbols(INamedTypeSymbol defaultOverloadAttribute, INamedTypeSymbol transformOverloadAttribute)
    {
        DefaultOverloadAttribute = defaultOverloadAttribute;
        TransformOverloadAttribute = transformOverloadAttribute;
    }

    public static KnownSymbols? TryCreateInstance(Compilation compilation)
    {
        var defaultAttribute =
            compilation.GetTypeByMetadataName(DefaultOverloadAttributeName);
        var transformAttribute =
            compilation.GetTypeByMetadataName(
                TransformOverloadAttributeName);

        if (defaultAttribute == null || transformAttribute == null)
            return null;

        return new KnownSymbols(defaultAttribute, transformAttribute);
    }
}