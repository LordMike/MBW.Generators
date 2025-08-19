using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.Common.Helpers;

public static class HashHelper
{
    public static void HashTypeParameters(this ref HashCode hc, ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        foreach (ITypeParameterSymbol? tp in typeParameters)
        {
            hc.Add(tp.Ordinal);
            hc.Add(tp.Variance); // None / In / Out
            hc.Add(tp.HasReferenceTypeConstraint);
            hc.Add(tp.HasValueTypeConstraint);
            hc.Add(tp.HasUnmanagedTypeConstraint);
            hc.Add(tp.HasNotNullConstraint);
            hc.Add(tp.HasConstructorConstraint);

            // Constraint types (order is stable in Roslyn)
            foreach (ITypeSymbol? ct in tp.ConstraintTypes)
                hc.HashTypeIdentity(ct);
        }
    }

    public static void HashTypeIdentity(this ref HashCode hc, ITypeSymbol t)
    {
        hc.Add(t.TypeKind);

        // Type parameters: identity by position within the method
        if (t is ITypeParameterSymbol tp)
        {
            hc.Add("tp");
            hc.Add(tp.Ordinal);
            return;
        }

        // Arrays
        if (t is IArrayTypeSymbol at)
        {
            hc.Add("arr");
            hc.Add(at.Rank);
            hc.HashTypeIdentity(at.ElementType);
            return;
        }

        // Pointers
        if (t is IPointerTypeSymbol pt)
        {
            hc.Add("ptr");
            hc.HashTypeIdentity(pt.PointedAtType);
            return;
        }

        // Named types (includes tuples/generics)
        if (t is INamedTypeSymbol nt)
        {
            // Namespace chain (outer -> inner)
            Stack<string> nsParts = new Stack<string>();
            for (INamespaceSymbol? ns = nt.ContainingNamespace; ns != null && !ns.IsGlobalNamespace; ns = ns.ContainingNamespace)
                nsParts.Push(ns.MetadataName);
            foreach (string? part in nsParts)
                hc.Add(part, StringComparer.Ordinal);

            // Containing types (outer -> inner), then this type
            Stack<INamedTypeSymbol> typeStack = new Stack<INamedTypeSymbol>();
            for (INamedTypeSymbol? cur = nt; cur != null; cur = cur.ContainingType)
                typeStack.Push(cur);
            while (typeStack.Count > 0)
            {
                INamedTypeSymbol? cur = typeStack.Pop();
                hc.Add(cur.MetadataName, StringComparer.Ordinal); // includes `N for arity
                hc.Add(cur.Arity);
            }

            // Generic type arguments (constructed type)
            if (nt.IsGenericType)
            {
                foreach (ITypeSymbol? arg in nt.TypeArguments)
                    hc.HashTypeIdentity(arg);
            }

            // Tuple element types (ensure shape equality)
            if (nt.IsTupleType)
            {
                hc.Add("tuple");
                foreach (IFieldSymbol? el in nt.TupleElements)
                    hc.HashTypeIdentity(el.Type);
            }
        }
    }

    /// <summary>
    /// Structural identity for named types: namespace chain, containing types, name, arity, kind.
    /// </summary>
    public static void HashTypeIdentity(this ref HashCode hc, INamedTypeSymbol t)
    {
        // Namespace chain (outer -> inner)
        Stack<string> nsParts = new Stack<string>();
        for (INamespaceSymbol? ns = t.ContainingNamespace; ns != null && !ns.IsGlobalNamespace; ns = ns.ContainingNamespace)
            nsParts.Push(ns.MetadataName);
        foreach (string? part in nsParts)
            hc.Add(part, StringComparer.Ordinal);

        // Containing types (outer -> inner), then this type
        Stack<INamedTypeSymbol> typeStack = new Stack<INamedTypeSymbol>();
        for (INamedTypeSymbol? cur = t; cur != null; cur = cur.ContainingType)
            typeStack.Push(cur);
        while (typeStack.Count > 0)
        {
            INamedTypeSymbol? cur = typeStack.Pop();
            hc.Add(cur.MetadataName, StringComparer.Ordinal); // includes `N for arity in metadata name when applicable
            hc.Add(cur.Arity);
            hc.Add(cur.TypeKind);
        }
    }

    public static int HashConstant(object? value)
    {
        if (value is null) return 0;

        // Handle primitives deterministically without culture
        switch (value)
        {
            case bool b: return b ? 1 : 2;
            case char ch: return ch;
            case string s: return StringComparer.Ordinal.GetHashCode(s);

            case sbyte v: return v;
            case byte v: return v;
            case short v: return v;
            case ushort v: return v;
            case int v: return v;
            case uint v: return unchecked((int)v);
            case long v: return v.GetHashCode();
            case ulong v: return v.GetHashCode();

            case float v: return v.GetHashCode();
            case double v: return v.GetHashCode();
            case decimal v: return v.GetHashCode();

            default: return value.GetHashCode();
        }
    }
}