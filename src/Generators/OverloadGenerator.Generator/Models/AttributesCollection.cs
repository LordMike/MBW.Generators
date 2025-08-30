using System.Collections.Generic;
using System.Collections.Immutable;
using MBW.Generators.OverloadGenerator.Generator.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Generator.Models;

internal readonly record struct AttributesCollection(
    ImmutableArray<DefaultOverloadAttributeInfoWithRegex> DefaultAttributes,
    ImmutableArray<TransformOverloadAttributeInfoWithRegex> TransformAttributes)
{
    public static AttributesCollection From(KnownSymbols? knownSymbols, ISymbol symbol)
    {
        if (knownSymbols is null)
            return new AttributesCollection(ImmutableArray<DefaultOverloadAttributeInfoWithRegex>.Empty,
                ImmutableArray<TransformOverloadAttributeInfoWithRegex>.Empty);

        List<DefaultOverloadAttributeInfoWithRegex>? defaults = null;
        List<TransformOverloadAttributeInfoWithRegex>? transforms = null;

        foreach (AttributeData attr in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, knownSymbols.DefaultOverloadAttribute))
            {
                var info = AttributeConverters.ToDefault(attr);
                if (AttributeValidation.IsValidRegexPattern(info.ParameterNamePattern, out var paramRegex) &&
                    AttributeValidation.IsValidRegexPattern(info.MethodNamePattern, out var methodRegex))
                {
                    defaults ??= new();
                    defaults.Add(new DefaultOverloadAttributeInfoWithRegex(info, paramRegex, methodRegex));
                }
            }
            else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, knownSymbols.TransformOverloadAttribute))
            {
                var info = AttributeConverters.ToTransform(attr);
                if (AttributeValidation.IsValidRegexPattern(info.ParameterNamePattern, out var paramRegex) &&
                    AttributeValidation.IsValidRegexPattern(info.MethodNamePattern, out var methodRegex))
                {
                    transforms ??= new();
                    transforms.Add(new TransformOverloadAttributeInfoWithRegex(info, paramRegex, methodRegex));
                }
            }
        }

        return new AttributesCollection(
            defaults == null ? ImmutableArray<DefaultOverloadAttributeInfoWithRegex>.Empty : [..defaults],
            transforms == null ? ImmutableArray<TransformOverloadAttributeInfoWithRegex>.Empty : [..transforms]);
    }
}
