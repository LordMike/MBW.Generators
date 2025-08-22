using System.Collections.Generic;
using System.Collections.Immutable;
using MBW.Generators.Common.Helpers;
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

            var providedExceptionType = info.ExceptionType as INamedTypeSymbol;

            if (info.ExceptionType == null ||
                (
                    providedExceptionType != null &&
                    providedExceptionType.IsDerivedFrom(knownSymbols.ExceptionBase)
                ))
            {
                if (AttributeValidation.IsValidRegexPattern(info.MethodNamePattern, out var methodNamePatternRegex))
                {
                    // Fall back to InvalidOperationException
                    var exceptionType = providedExceptionType ?? knownSymbols.InvalidOperationException;

                    res ??= [];
                    res.Add(new GenerateNonTryMethodAttributeInfoWithValidPattern(info, exceptionType,
                        methodNamePatternRegex));
                }
            }
        }

        if (res == null)
            return ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern>.Empty;

        return [..res];
    }
}