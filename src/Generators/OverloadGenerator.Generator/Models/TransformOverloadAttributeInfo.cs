using MBW.Generators.OverloadGenerator.Attributes;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Generator.Models;

internal readonly record struct TransformOverloadAttributeInfo(
    Location Location,
    string ParameterNamePattern,
    INamedTypeSymbol? ParameterType,
    INamedTypeSymbol NewType,
    string TransformExpression,
    TypeNullability NewTypeNullability,
    string MethodNamePattern,
    string? MethodNameReplace)
{
    public Location Location { get; } = Location;
    public string ParameterNamePattern { get; } = ParameterNamePattern;
    public INamedTypeSymbol? ParameterType { get; } = ParameterType;
    public INamedTypeSymbol NewType { get; } = NewType;
    public string TransformExpression { get; } = TransformExpression;
    public TypeNullability NewTypeNullability { get; } = NewTypeNullability;
    public string MethodNamePattern { get; } = MethodNamePattern;
    public string? MethodNameReplace { get; } = MethodNameReplace;
}
