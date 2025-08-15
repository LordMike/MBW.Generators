using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.NonTryMethods;

internal static class Display
{
    public static readonly SymbolDisplayFormat Fqn = SymbolDisplayFormat.FullyQualifiedFormat;
    public static readonly SymbolDisplayFormat Minimal = SymbolDisplayFormat.MinimallyQualifiedFormat;

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

    public static NameSyntax? GlobalNamespaceName(INamespaceSymbol? ns)
        => ns is null || ns.IsGlobalNamespace
            ? null
            : SyntaxFactory.ParseName(ns.ToDisplayString(Fqn));

    public static TypeSyntax GlobalType(ITypeSymbol t)
        => SyntaxFactory.ParseTypeName(t.ToDisplayString(Fqn));

    public static string MinimalText(ISymbol s)
        => s.ToDisplayString(Minimal);
}