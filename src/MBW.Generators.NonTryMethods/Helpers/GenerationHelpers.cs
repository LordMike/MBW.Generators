using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MBW.Generators.NonTryMethods.GenerationModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace MBW.Generators.NonTryMethods.Helpers;

internal class GenerationHelpers
{
    public static ExpressionSyntax ToCSharpLiteralExpression(object? value)
    {
        if (value is null)
            return LiteralExpression(SyntaxKind.NullLiteralExpression);
        ;
        if (value is string s)
            return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(s));
        if (value is char ch)
            return LiteralExpression(SyntaxKind.CharacterLiteralExpression, Literal(ch));
        return ParseExpression(SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false));
    }

    public static string FindUnusedParamName(ImmutableArray<IParameterSymbol> @params, string prefix)
    {
        HashSet<string> reserved = new HashSet<string>(@params.Select(p => p.Name), StringComparer.Ordinal);
        string name = prefix;
        int i = 1;
        while (reserved.Contains(name)) name = prefix + i++;
        return name;
    }

    public static ImmutableArray<UsingDirectiveSyntax> GetUsingsForType(
        INamedTypeSymbol type)
    {
        var seen = new HashSet<string>();
        var result = new List<UsingDirectiveSyntax>();

        var systemUsing = UsingDirective(IdentifierName("System"))
            .WithTrailingTrivia(ElasticCarriageReturnLineFeed);
        AddIfNew(systemUsing);

        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            var @namespace = UsingDirective(type.ContainingNamespace.RenderNamespaceName()!)
                .WithTrailingTrivia(ElasticCarriageReturnLineFeed);
            AddIfNew(@namespace);
        }

        // Optionally gather global usings once (applies to whole compilation)
        foreach (var declRef in type.DeclaringSyntaxReferences)
        {
            if (declRef.GetSyntax() is not TypeDeclarationSyntax decl)
                continue;

            // 1) File-level (top-level) usings
            var cu = decl.SyntaxTree.GetCompilationUnitRoot();
            foreach (var u in cu.Usings.Where(u => u.GlobalKeyword.IsKind(SyntaxKind.None)))
                AddIfNew(u);

            // 2) Usings on enclosing namespaces (closest first -> outermost)
            for (SyntaxNode? cur = decl.Parent; cur is not null; cur = cur.Parent)
            {
                if (cur is BaseNamespaceDeclarationSyntax ns)
                {
                    // Namespace usings come before child members; they are all in scope here
                    foreach (var u in ns.Usings)
                        AddIfNew(u);
                }
            }
        }

        return result.ToImmutableArray();

        void AddIfNew(UsingDirectiveSyntax u, bool isGlobal = false)
        {
            // Build a simple identity key to de-dupe:
            //   - alias name (if any)
            //   - static vs normal
            //   - target name text
            //   - global vs non-global
            var alias = u.Alias?.Name.Identifier.ValueText ?? "";
            var isStatic = u.StaticKeyword.Kind() != SyntaxKind.None ? "static" : "";
            var name = u.Name?.ToString() ?? ""; // includes qualified name text
            var globalMark = isGlobal || u.GlobalKeyword.Kind() != SyntaxKind.None ? "global" : "";

            var key = $"{alias}|{isStatic}|{globalMark}|{name}";
            if (seen.Add(key))
                result.Add(u);
        }
    }

    public static MinimalStringInfo GetSourceDisplayContext(Compilation comp, ISymbol symbol)
    {
        var decl = symbol.DeclaringSyntaxReferences.First().GetSyntax();
        var tree = decl.SyntaxTree;
        var model = comp.GetSemanticModel(tree, ignoreAccessibility: true);

        var pos = decl switch
        {
            Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax m => m.Identifier.SpanStart,
            Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax t => t.Identifier.SpanStart,
            _ => decl.SpanStart
        };
        return new(model, pos);
    }
}