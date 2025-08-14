using System.Collections.Generic;
using System.Collections.Immutable;
using MBW.Generators.NonTryMethods.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal static class AttributesCollection
{
    public static ImmutableArray<GenerateNonTryMethodAttributeInfo> From(KnownSymbols? knownSymbols,
        ISymbol symbol)
    {
        if (knownSymbols is null)
            return ImmutableArray<GenerateNonTryMethodAttributeInfo>.Empty;

        List<GenerateNonTryMethodAttributeInfo>? res = null;
        foreach (AttributeData attr in symbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, knownSymbols.GenerateNonTryMethodAttribute))
                continue;

            var info = AttributeConverters.ToNonTry(attr);
            res ??= [];
            res.Add(info);
        }

        if (res == null)
            return ImmutableArray<GenerateNonTryMethodAttributeInfo>.Empty;

        return [..res];
    }
}