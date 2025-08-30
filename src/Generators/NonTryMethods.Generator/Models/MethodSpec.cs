using System;
using System.Collections.Immutable;
using MBW.Generators.Common.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Generator.Models;

internal sealed class MethodSpec : IEquatable<MethodSpec>
{
    public readonly int Key;
    public readonly IMethodSymbol Method;
    public readonly ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> ApplicableAttributes;

    public MethodSpec(IMethodSymbol method,
        ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> applicableAttributes)
    {
        Method = method;
        ApplicableAttributes = applicableAttributes;

        var hc = new HashCode();

        // Overload identity + things that affect the generated text
        hc.Add(method.Name, StringComparer.Ordinal);
        hc.Add(method.Arity); // generic method arity
        hc.Add(method.DeclaredAccessibility); // you mirror this in emit
        hc.Add(method.IsStatic); // affects call target and modifiers

        // Extension 'this' changes generated signature (you re-emit "this" on first param)
        if (method.Parameters.Length > 0)
            hc.Add(method.IsExtensionMethod && method.Parameters[0].IsThis);

        // Return type (not part of signature, but you print it)
        hc.HashTypeIdentity(method.ReturnType);

        // Parameters (ordered): ref-kind + type identity (+ params/defaults since you reprint them)
        foreach (var p in method.Parameters)
        {
            hc.Add(p.RefKind); // ref / out / in affect signature
            hc.HashTypeIdentity(p.Type); // full structural identity
            hc.Add(p.IsParams); // affects printed signature
            hc.Add(p.HasExplicitDefaultValue); // affects printed signature
            if (p.HasExplicitDefaultValue)
                hc.Add(HashHelper.HashConstant(p.ExplicitDefaultValue));
        }

        // Generic constraints (you re-emit where-clauses)
        hc.HashTypeParameters(method.TypeParameters);

        Key = hc.ToHashCode();
    }

    public bool Equals(MethodSpec? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key;
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is MethodSpec other && Equals(other);
    public override int GetHashCode() => Key;
}