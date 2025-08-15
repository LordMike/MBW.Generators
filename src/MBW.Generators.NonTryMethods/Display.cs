using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.NonTryMethods;

internal static class Display
{
    public static readonly SymbolDisplayFormat Fqn = SymbolDisplayFormat.FullyQualifiedFormat;
    public static readonly SymbolDisplayFormat Minimal = SymbolDisplayFormat.MinimallyQualifiedFormat;

    public static NameSyntax? GlobalNamespaceName(INamespaceSymbol? ns)
        => ns is null || ns.IsGlobalNamespace
            ? null
            : SyntaxFactory.ParseName(ns.ToDisplayString(Fqn));

    public static TypeSyntax GlobalType(ITypeSymbol t)
        => SyntaxFactory.ParseTypeName(t.ToDisplayString(Fqn));

    public static string MinimalText(ISymbol s)
        => s.ToDisplayString(Minimal);
}