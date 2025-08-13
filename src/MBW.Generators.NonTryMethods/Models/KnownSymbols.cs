using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed class KnownSymbols
{
    public readonly INamedTypeSymbol GenerateNonTryMethodAttribute;
    public readonly INamedTypeSymbol GenerateNonTryAsyncOptionsAttribute;
    public readonly INamedTypeSymbol? TaskOfT;
    public readonly INamedTypeSymbol? ValueTaskOfT;

    private KnownSymbols(INamedTypeSymbol generateNonTryMethodAttribute,
        INamedTypeSymbol generateNonTryAsyncOptionsAttribute, INamedTypeSymbol? taskOfT,
        INamedTypeSymbol? valueTaskOfT)
    {
        GenerateNonTryMethodAttribute = generateNonTryMethodAttribute;
        GenerateNonTryAsyncOptionsAttribute = generateNonTryAsyncOptionsAttribute;
        TaskOfT = taskOfT;
        ValueTaskOfT = valueTaskOfT;
    }

    public static KnownSymbols? CreateInstance(Compilation c)
    {
        var generateNonTryMethodAttribute =
            c.GetTypeByMetadataName("MBW.Generators.NonTryMethods.Attributes.GenerateNonTryMethodAttribute");
        var generateNonTryAsyncOptionsAttribute =
            c.GetTypeByMetadataName(
                "MBW.Generators.NonTryMethods.Attributes.GenerateNonTryAsyncOptionsAttribute");
        var taskOfT = c.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskOfT = c.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        if (generateNonTryMethodAttribute == null || generateNonTryAsyncOptionsAttribute == null)
            return null;

        return new KnownSymbols(generateNonTryMethodAttribute, generateNonTryAsyncOptionsAttribute, taskOfT,
            valueTaskOfT);
    }
}