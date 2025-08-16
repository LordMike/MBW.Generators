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

    public static MinimalStringInfo GetSourceDisplayContext(Compilation comp, ISymbol symbol)
    {
        var decl = symbol.DeclaringSyntaxReferences.First().GetSyntax();
        var tree = decl.SyntaxTree;
        var model = comp.GetSemanticModel(tree, ignoreAccessibility: true);

        var pos = decl switch
        {
            MethodDeclarationSyntax m => m.Identifier.SpanStart,
            TypeDeclarationSyntax t => t.Identifier.SpanStart,
            _ => decl.SpanStart
        };
        return new(model, pos);
    }
}