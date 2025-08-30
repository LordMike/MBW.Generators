using System;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Generator.Models;

internal sealed class KnownSymbols
{
    public const string NonTryAttribute = "MBW.Generators.NonTryMethods.Attributes.GenerateNonTryMethodAttribute";
    public const string NonTryOptionsAttribute = "MBW.Generators.NonTryMethods.Attributes.GenerateNonTryOptionsAttribute";
   
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

    public static KnownSymbols? TryCreateInstance(Compilation compilation)
    {
        var generateNonTryMethodAttribute =
            compilation.GetTypeByMetadataName(NonTryAttribute);
        var generateNonTryOptionsAttribute =
            compilation.GetTypeByMetadataName(
                NonTryOptionsAttribute);

        if (generateNonTryMethodAttribute == null || generateNonTryOptionsAttribute == null)
            return null;

        return new KnownSymbols(compilation, generateNonTryMethodAttribute, generateNonTryOptionsAttribute);
    }
}