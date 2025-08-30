using MBW.Generators.OverloadGenerator.Attributes;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Generator.Models;

sealed class TransformRule : Rule
{
    public TransformRule(string parameter, INamedTypeSymbol? accept, string transform, TypeNullability nullability)
        : base(parameter)
    {
        Accept = accept;
        Transform = transform;
        Nullability = nullability;
    }

    public INamedTypeSymbol? Accept { get; }
    public string Transform { get; }
    public TypeNullability Nullability { get; }
}