using System;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed class KnownSymbols
{
    public readonly INamedTypeSymbol GenerateNonTryMethodAttribute;
    public readonly INamedTypeSymbol GenerateNonTryOptionsAttribute;
    public readonly INamedTypeSymbol? TaskOfT;
    public readonly INamedTypeSymbol? ValueTaskOfT;

    public readonly INamedTypeSymbol ExceptionBase;
    public readonly INamedTypeSymbol InvalidOperationException;

    private KnownSymbols(Compilation compilation, INamedTypeSymbol generateNonTryMethodAttribute,
        INamedTypeSymbol generateNonTryOptionsAttribute)
    {
        GenerateNonTryMethodAttribute = generateNonTryMethodAttribute;
        GenerateNonTryOptionsAttribute = generateNonTryOptionsAttribute;
        TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        ValueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        ExceptionBase = compilation.GetTypeByMetadataName("System.Exception") ??
                        throw new InvalidOperationException("Unable to locate System.Exception in compilation");
        InvalidOperationException = compilation.GetTypeByMetadataName("System.InvalidOperationException") ??
                                    throw new InvalidOperationException(
                                        "Unable to locate System.InvalidOperationException in compilation");
    }

    public static KnownSymbols? CreateInstance(Compilation compilation)
    {
        var generateNonTryMethodAttribute =
            compilation.GetTypeByMetadataName("MBW.Generators.NonTryMethods.Attributes.GenerateNonTryMethodAttribute");
        var generateNonTryOptionsAttribute =
            compilation.GetTypeByMetadataName(
                "MBW.Generators.NonTryMethods.Attributes.GenerateNonTryOptionsAttribute");

        if (generateNonTryMethodAttribute == null || generateNonTryOptionsAttribute == null)
            return null;

        return new KnownSymbols(compilation, generateNonTryMethodAttribute, generateNonTryOptionsAttribute);
    }
}