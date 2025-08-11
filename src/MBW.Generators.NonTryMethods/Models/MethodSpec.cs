using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed class MethodSpec(
    IMethodSymbol method,
    ImmutableArray<(GenerateNonTryMethodAttributeInfo info, Location location)> applicableAttributes)
{
    public readonly IMethodSymbol Method = method;
    public readonly ImmutableArray<(GenerateNonTryMethodAttributeInfo info, Location location)> ApplicableAttributes = applicableAttributes;

    private sealed class MethodApplicableAttributesEqualityComparer : IEqualityComparer<MethodSpec>
    {
        public bool Equals(MethodSpec? x, MethodSpec? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Method.Equals(y.Method, SymbolEqualityComparer.Default) &&
                   x.ApplicableAttributes.Equals(y.ApplicableAttributes);
        }

        public int GetHashCode(MethodSpec obj)
        {
            unchecked
            {
                return (SymbolEqualityComparer.Default.GetHashCode(obj.Method) * 397) ^
                       obj.ApplicableAttributes.GetHashCode();
            }
        }
    }

    public static IEqualityComparer<MethodSpec> Comparer { get; } = new MethodApplicableAttributesEqualityComparer();
}