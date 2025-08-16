using System.Linq;
using MBW.Generators.NonTryMethods.GenerationModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.NonTryMethods.Helpers;

internal static class Extensions
{
    private static readonly SymbolDisplayFormat NamespaceFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    public static NameSyntax? RenderNamespaceName(this INamespaceSymbol ns)
    {
        if (ns.IsGlobalNamespace)
            return null;

        return SyntaxFactory.ParseName(ns.ToDisplayString(NamespaceFormat));
    }

    public static bool IsPartial(this INamedTypeSymbol type)
    {
        foreach (SyntaxReference? r in type.DeclaringSyntaxReferences)
        {
            if (r.GetSyntax() is TypeDeclarationSyntax tds &&
                tds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return true;
        }

        return false;
    }

    public static bool IsDerivedFrom(this INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (INamedTypeSymbol? cur = type; cur is not null; cur = cur.BaseType)
            if (SymbolEqualityComparer.Default.Equals(cur, baseType))
                return true;
        return false;
    }

    public static string ToMinimalDisplayString(this ISymbol symbol,
        MinimalStringInfo info,
        SymbolDisplayFormat? format = null)
    {
        return symbol.ToMinimalDisplayString(info.SemanticModel, info.Position, format);
    }
}