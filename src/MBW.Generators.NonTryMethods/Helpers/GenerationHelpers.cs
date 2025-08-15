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

    public static IEnumerable<UsingDirectiveSyntax> GetUsingsForType(INamedTypeSymbol type, Compilation compilation)
    {
        // First: global + implicit usings from the compilation
        var allUsings = new List<UsingDirectiveSyntax>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            allUsings.AddRange(root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Where(u => u.GlobalKeyword != default)); // global usings only
        }

        // Then: usings in the file where the type is declared
        var declRef = type.DeclaringSyntaxReferences.FirstOrDefault();
        if (declRef != null)
        {
            var syntax = declRef.GetSyntax();
            var tree = syntax.SyntaxTree;
            var root = tree.GetRoot();

            // All top-level usings in this file
            allUsings.AddRange(root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Where(u => u.Parent is CompilationUnitSyntax));

            // Usings inside the namespace that directly contains this type
            var nsNode = syntax.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
            if (nsNode != null)
                allUsings.AddRange(nsNode.Usings);
        }

        return allUsings;
    }
}