using Microsoft.CodeAnalysis;

namespace MBW.Generators.Common;

internal static class DisplayFormats
{
    public static readonly SymbolDisplayFormat CrefFormat =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType |
                           SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                              SymbolDisplayParameterOptions.IncludeModifiers,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                                  SymbolDisplayMiscellaneousOptions.UseSpecialTypes
        );

    public static readonly SymbolDisplayFormat NullableQualifiedFormat =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions:
            SymbolDisplayGenericsOptions.IncludeTypeParameters |
            SymbolDisplayGenericsOptions.IncludeVariance,
            miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes | // int, string, etc.
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | // adds ?
            SymbolDisplayMiscellaneousOptions.ExpandNullable, // Nullable<T> -> T?
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions:
            SymbolDisplayParameterOptions.IncludeType |
            SymbolDisplayParameterOptions.IncludeParamsRefOut |
            SymbolDisplayParameterOptions.IncludeDefaultValue |
            SymbolDisplayParameterOptions.IncludeName,
            localOptions: SymbolDisplayLocalOptions.None
        );
}