using Microsoft.CodeAnalysis;

namespace MBW.Generators.GeneratorHelpers.Benchmarks;

static class ManualComparer
{
    public static bool IsNamedExactlyType_SpanVersion(ISymbol? symbol, string fullyQualifiedWithGlobal)
    {
        if (symbol is not INamedTypeSymbol named)
            return false;

        ReadOnlySpan<char> span = fullyQualifiedWithGlobal.AsSpan();

        const string globalPrefix = "global::";
        if (span.StartsWith(globalPrefix.AsSpan(), StringComparison.Ordinal))
            span = span.Slice(globalPrefix.Length);

        int lastDot = span.LastIndexOf('.');
        if (lastDot < 0) return false;

        ReadOnlySpan<char> nsSpan = span[..lastDot];
        ReadOnlySpan<char> typeSpan = span[(lastDot + 1)..];

        // Handle nested types: FullyQualifiedFormat uses '.' between nested type segments
        var current = named;
        while (true)
        {
            int dot = typeSpan.LastIndexOf('.');
            ReadOnlySpan<char> seg = dot >= 0 ? typeSpan[(dot + 1)..] : typeSpan;

            if (!seg.Equals(current.MetadataName.AsSpan(), StringComparison.Ordinal))
                return false;

            if (dot < 0) break; // matched outermost type name

            current = current.ContainingType;
            if (current is null)
                return false;

            typeSpan = typeSpan[..dot];
        }

        // No extra containing types on the symbol
        if (current.ContainingType is not null) return false;

        // Compare namespace segments right-to-left
        var ns = named.ContainingNamespace;
        while (!nsSpan.IsEmpty)
        {
            int dot = nsSpan.LastIndexOf('.');
            ReadOnlySpan<char> seg = dot >= 0 ? nsSpan[(dot + 1)..] : nsSpan;

            if (ns is null || ns.IsGlobalNamespace) return false;
            if (!seg.Equals(ns.Name.AsSpan(), StringComparison.Ordinal))
                return false;

            ns = ns.ContainingNamespace;
            nsSpan = dot >= 0 ? nsSpan[..dot] : ReadOnlySpan<char>.Empty;
        }

        return ns is { IsGlobalNamespace: true };
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="symbol"/> is exactly <c>MBW.Generators.OverloadGenerator.Attributes.TransformOverloadAttribute</c>.</summary>
    /// <param name="symbol">Symbol to check.</param>
    /// <returns><see langword="true"/> if <paramref name="symbol"/> is <c>MBW.Generators.OverloadGenerator.Attributes.TransformOverloadAttribute</c>.</returns>
    public static bool IsNamedExactlyType_GeneratedByGenerator(this ISymbol? symbol)
    {
        if (symbol is null)
            return false;
        if (!symbol.Name.Equals("TransformOverloadAttribute", StringComparison.Ordinal))
            return false;
        if (symbol is not INamedTypeSymbol t0)
            return false;
        if (t0.ContainingType is not null)
            return false;
                                   
        var ns = t0.ContainingNamespace;
        if (ns is null || !ns.Name.Equals("Attributes", StringComparison.Ordinal))
            return false;
        ns = ns.ContainingNamespace;
        if (ns is null || !ns.Name.Equals("OverloadGenerator", StringComparison.Ordinal))
            return false;
        ns = ns.ContainingNamespace;
        if (ns is null || !ns.Name.Equals("Generators", StringComparison.Ordinal))
            return false;
        ns = ns.ContainingNamespace;
        if (ns is null || !ns.Name.Equals("MBW", StringComparison.Ordinal))
            return false;
        ns = ns.ContainingNamespace;
        return ns != null && ns.IsGlobalNamespace;
    }
}