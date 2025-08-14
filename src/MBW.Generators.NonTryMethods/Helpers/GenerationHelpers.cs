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
}