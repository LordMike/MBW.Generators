using System.Collections.Generic;
using System.Collections.Immutable;
using MBW.Generators.Common.Helpers;
using MBW.Generators.NonTryMethods.Generator.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Generator.Models;

internal static class AttributesCollection
{
    public static ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> From(Compilation compilation,
        ISymbol symbol)
    {
        var exceptionBase = compilation.GetTypeByMetadataName(KnownSymbols.ExceptionBase);
        var invalidOperation = compilation.GetTypeByMetadataName(KnownSymbols.InvalidOperationException);

        if (exceptionBase is null || invalidOperation is null)
            return ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern>.Empty;

        List<GenerateNonTryMethodAttributeInfoWithValidPattern>? res = null;
        foreach (AttributeData attr in symbol.GetAttributes())
        {
            if (!attr.AttributeClass.IsNamedExactlyTypeGenerateNonTryMethodAttribute())
                continue;

            var info = AttributeConverters.ToNonTry(attr);

            var providedExceptionType = info.ExceptionType as INamedTypeSymbol;

            if (info.ExceptionType == null ||
                (
                    providedExceptionType != null &&
                    providedExceptionType.IsDerivedFrom(exceptionBase)
                ))
            {
                if (AttributeValidation.IsValidRegexPattern(info.MethodNamePattern, out var methodNamePatternRegex))
                {
                    var exceptionType = providedExceptionType ?? invalidOperation;

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