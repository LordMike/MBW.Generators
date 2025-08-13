using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.NonTryMethods.Helpers;

internal static class Extensions
{
    public static bool IsPartial(this INamedTypeSymbol type)
    {
        foreach (var r in type.DeclaringSyntaxReferences)
        {
            if (r.GetSyntax() is TypeDeclarationSyntax tds &&
                tds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return true;
        }

        return false;
    }
}