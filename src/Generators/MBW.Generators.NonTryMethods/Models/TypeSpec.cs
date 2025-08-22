using System;
using System.Collections.Immutable;
using MBW.Generators.Common;
using MBW.Generators.Common.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed class TypeSpec : IEquatable<TypeSpec>
{
    public readonly INamedTypeSymbol Type;
    public readonly int Key;
    public readonly KnownSymbols Symbols;
    public readonly ImmutableArray<MethodSpec> Methods;

    public TypeSpec(KnownSymbols symbols, INamedTypeSymbol type, ImmutableArray<MethodSpec> methods)
    {
        Symbols = symbols;
        Type = type;
        Methods = methods;

        // Structural identity
        var hc = new HashCode();

        hc.HashTypeIdentity(type);

        // Emit-relevant modifiers
        hc.Add(type.DeclaredAccessibility);
        hc.Add(type.IsStatic);
        hc.Add(type.TypeKind);
        hc.Add(type.IsPartial());
        hc.Add(type.IsAbstract && type.TypeKind == TypeKind.Class);
        hc.Add(type.IsSealed && type.TypeKind == TypeKind.Class);

        // Type parameters (affect emitted method signatures)
        hc.HashTypeParameters(type.TypeParameters);

        // Methods
        foreach (var methodSpec in methods)
            hc.Add(methodSpec.Key);

        Key = hc.ToHashCode();
    }

    public bool Equals(TypeSpec? other)
    {
        var res = Equals2(other);
        Logger.Log($"Equality called, this key: {Key}, other: {other?.Key}, res: {res}");
        return res;
    }

    private bool Equals2(TypeSpec? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key;
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is TypeSpec other && Equals(other);
    public override int GetHashCode() => Key;
}