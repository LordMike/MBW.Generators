using System.Collections.Generic;
using System.Collections.Immutable;
using MBW.Generators.NonTryMethods.Generator.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Generator.Models;

internal static class AttributesCollection
{
    public static ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> From(ISymbol symbol)
    {
        List<GenerateNonTryMethodAttributeInfoWithValidPattern>? res = null;
        foreach (AttributeData attr in symbol.GetAttributes())
        {
            if (!attr.AttributeClass.IsNamedExactlyTypeGenerateNonTryMethodAttribute())
                continue;

            var info = AttributeConverters.ToNonTry(attr);
            var providedExceptionType = info.ExceptionType as INamedTypeSymbol;

            if (info.ExceptionType == null ||
                (providedExceptionType != null && IsDerivedFromException(providedExceptionType)))
            {
                if (AttributeValidation.IsValidRegexPattern(info.MethodNamePattern, out var methodNamePatternRegex))
                {
                    var exceptionName = info.ExceptionTypeName ?? KnownSymbols.InvalidOperationException;

                    res ??= [];
                    res.Add(new GenerateNonTryMethodAttributeInfoWithValidPattern(exceptionName, info.MethodNamePattern, methodNamePatternRegex));
                }
            }
        }

        if (res == null)
            return ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern>.Empty;

        return [..res];

        static bool IsDerivedFromException(INamedTypeSymbol type)
        {
            for (INamedTypeSymbol? cur = type; cur is not null; cur = cur.BaseType)
                if (cur.IsNamedExactlyTypeExceptionBase())
                    return true;
            return false;
        }
    }
}
