using System.Collections.Generic;
using System.Collections.Immutable;
using MBW.Generators.NonTryMethods.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal static class AttributesCollection
{
    public static ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> From(KnownSymbols? knownSymbols,
        ISymbol symbol)
    {
        if (knownSymbols is null)
            return ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern>.Empty;

        List<GenerateNonTryMethodAttributeInfoWithValidPattern>? res = null;
        foreach (AttributeData attr in symbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, knownSymbols.GenerateNonTryMethodAttribute))
                continue;

            var info = AttributeConverters.ToNonTry(attr);

            if (AttributeValidation.IsValidRegexPattern(info.MethodNamePattern))
            {
                res ??= [];
                res.Add(new GenerateNonTryMethodAttributeInfoWithValidPattern(info));
            }
        }

        if (res == null)
            return ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern>.Empty;

        return [..res];
    }
}