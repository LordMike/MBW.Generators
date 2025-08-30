using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace MBW.Generators.Common.Helpers;

internal class GenerationHelpers
{
    internal static string GetHintName(string generatorName, INamedTypeSymbol type)
    {
        // A.B.Outer.Inner`1 -> A.B.Outer.Inner.NonTry.g.cs (strip arity on leaf)
        var simple = type.Name;
        var tick = simple.IndexOf('`');
        if (tick >= 0) simple = simple.Substring(0, tick);

        string ns = "";
        var nsSym = type.ContainingNamespace;
        if (nsSym is not null && !nsSym.IsGlobalNamespace)
        {
            ns = nsSym.ToDisplayString();
            if (ns.Length != 0) ns += ".";
        }

        return $"{ns}{simple}.{generatorName}.g.cs";
    }

    internal static ExpressionSyntax ToCSharpLiteralExpression(object? value)
    {
        if (value is null)
            return LiteralExpression(SyntaxKind.NullLiteralExpression);
        if (value is string s)
            return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(s));
        if (value is char ch)
            return LiteralExpression(SyntaxKind.CharacterLiteralExpression, Literal(ch));
        return ParseExpression(SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false));
    }

    internal static string FindUnusedParamName(ImmutableArray<IParameterSymbol> @params, string prefix)
    {
        HashSet<string> reserved = new HashSet<string>(@params.Select(p => p.Name), StringComparer.Ordinal);
        string name = prefix;
        int i = 1;
        while (reserved.Contains(name)) name = prefix + i++;
        return name;
    }
}