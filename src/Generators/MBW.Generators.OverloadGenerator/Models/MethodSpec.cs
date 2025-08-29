using System;
using System.Collections.Immutable;
using MBW.Generators.Common.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Models;

internal readonly record struct MethodSpec(IMethodSymbol Method, ImmutableArray<Rule> Rules)
{
    public int Key { get; } = ComputeKey(Method, Rules);

    private static int ComputeKey(IMethodSymbol method, ImmutableArray<Rule> rules)
    {
        var hc = new HashCode();
        hc.Add(method.Name, StringComparer.Ordinal);
        hc.Add(method.Arity);
        hc.Add(method.DeclaredAccessibility);
        hc.Add(method.IsStatic);
        if (method.Parameters.Length > 0)
            hc.Add(method.IsExtensionMethod && method.Parameters[0].IsThis);
        hc.HashTypeIdentity(method.ReturnType);
        foreach (var p in method.Parameters)
        {
            hc.Add(p.RefKind);
            hc.HashTypeIdentity(p.Type);
            hc.Add(p.IsParams);
            hc.Add(p.HasExplicitDefaultValue);
            if (p.HasExplicitDefaultValue)
                hc.Add(HashHelper.HashConstant(p.ExplicitDefaultValue));
        }
        hc.HashTypeParameters(method.TypeParameters);

        foreach (Rule r in rules)
        {
            hc.Add(r.Parameter, StringComparer.Ordinal);
            if (r is TransformRule tr)
            {
                hc.HashTypeIdentity(tr.Accept!);
                hc.Add(tr.Transform, StringComparer.Ordinal);
                hc.Add((int)tr.Nullability);
            }
            else if (r is DefaultRule dr)
            {
                hc.Add(dr.Expression, StringComparer.Ordinal);
            }
        }

        return hc.ToHashCode();
    }

    public bool Equals(MethodSpec other) => Key == other.Key;

    public override int GetHashCode() => Key;
}
