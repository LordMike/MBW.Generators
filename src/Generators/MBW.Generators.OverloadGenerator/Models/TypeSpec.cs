using System;
using System.Collections.Immutable;
using MBW.Generators.Common.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Models;

internal readonly record struct TypeSpec(KnownSymbols Symbols, INamedTypeSymbol Type, ImmutableArray<MethodSpec> Methods)
{
    public int Key { get; } = ComputeKey(Type, Methods);

    private static int ComputeKey(INamedTypeSymbol type, ImmutableArray<MethodSpec> methods)
    {
        var hc = new HashCode();
        hc.HashTypeIdentity(type);
        hc.Add(type.DeclaredAccessibility);
        hc.Add(type.IsStatic);
        hc.Add(type.TypeKind);
        hc.Add(type.IsPartial());
        hc.Add(type.IsAbstract && type.TypeKind == TypeKind.Class);
        hc.Add(type.IsSealed && type.TypeKind == TypeKind.Class);
        hc.HashTypeParameters(type.TypeParameters);
        foreach (var m in methods)
            hc.Add(m.Key);
        return hc.ToHashCode();
    }

    public bool Equals(TypeSpec other) => Key == other.Key;

    public override int GetHashCode() => Key;
}
