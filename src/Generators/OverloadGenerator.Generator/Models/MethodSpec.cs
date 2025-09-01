using System;
using System.Collections.Immutable;
using MBW.Generators.Common.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Generator.Models;

internal readonly record struct MethodSpec(IMethodSymbol Method, Rule Rule)
{
    public int Key { get; } = ComputeKey(Method, Rule);

    private static int ComputeKey(IMethodSymbol method, Rule rule)
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

        // Rule
        hc.Add(rule.Parameter, StringComparer.Ordinal);
        if (rule is TransformRule tr)
        {
            hc.HashTypeIdentity(tr.Accept!);
            hc.Add(tr.Transform, StringComparer.Ordinal);
            hc.Add((int)tr.Nullability);
        }
        else if (rule is DefaultRule dr)
        {
            hc.Add(dr.Expression, StringComparer.Ordinal);
        }

        return hc.ToHashCode();
    }

    public bool Equals(MethodSpec other) => Key == other.Key;

    public override int GetHashCode() => Key;
}