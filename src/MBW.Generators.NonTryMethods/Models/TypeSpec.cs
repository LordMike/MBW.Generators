using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed class TypeSpec(KnownSymbols symbols, INamedTypeSymbol type, ImmutableArray<MethodSpec> methods)
{
    public readonly KnownSymbols Symbols = symbols;
    public readonly INamedTypeSymbol Type = type;
    public readonly ImmutableArray<MethodSpec> Methods = methods;
    
    private sealed class TypeMethodsEqualityComparer : IEqualityComparer<TypeSpec>
    {
        public bool Equals(TypeSpec? x, TypeSpec? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Type.Equals(y.Type, SymbolEqualityComparer.Default) && x.Methods.Equals(y.Methods);
        }

        public int GetHashCode(TypeSpec obj)
        {
            unchecked
            {
                return (SymbolEqualityComparer.Default.GetHashCode(obj.Type) * 397) ^ obj.Methods.GetHashCode();
            }
        }
    }

    public static IEqualityComparer<TypeSpec> Comparer { get; } = new TypeMethodsEqualityComparer();
}