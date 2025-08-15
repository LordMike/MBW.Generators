using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace MBW.Generators.Tests.Common;

public static class SyntaxHelper
{
    public static void Equal(string expected, string? actual, ComparisonOptions options = ComparisonOptions.Default)
    {
        Assert.NotNull(actual);

        string normExpected = Normalize(expected, options);
        string normActual = Normalize(actual, options);

        Assert.Equal(normExpected, normActual);
    }

    private static string Normalize(string code, ComparisonOptions options)
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var tree = CSharpSyntaxTree.ParseText(code, parse);

        // 1) Normalize trivia via Roslyn (aggressively regularizes whitespace/newlines)
        var root = tree.GetRoot().NormalizeWhitespace(elasticTrivia: true);

        // Strip all XML doc comments from trivia
        if ((options & ComparisonOptions.RemoveXmlDocs) != 0)
        {
            root = root.ReplaceTrivia(
                root.DescendantTrivia(descendIntoTrivia: true),
                (orig, _) =>
                {
                    if (orig.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                        orig.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                    {
                        return default; // remove
                    }

                    return orig;
                });
        }

        // 2) Convert to text and apply final, deterministic whitespace canonicalization
        var text = root.ToFullString();

        // Force LF endings
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // Trim trailing spaces per line
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]+\n", "\n");

        // Collapse 3+ blank lines to a single blank line
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");

        // Optional: remove leading/trailing blank lines
        text = text.Trim('\n', ' ', '\t');

        return text;
    }

    [Flags]
    public enum ComparisonOptions
    {
        None,
        RemoveXmlDocs = 1,
        Default = RemoveXmlDocs
    }
}