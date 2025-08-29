using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Models;

internal readonly record struct DefaultOverloadAttributeInfo(
    Location Location,
    string ParameterNamePattern,
    INamedTypeSymbol? ParameterType,
    string DefaultExpression,
    string MethodNamePattern,
    string? MethodNameReplace)
{
    public Location Location { get; } = Location;
    public string ParameterNamePattern { get; } = ParameterNamePattern;
    public INamedTypeSymbol? ParameterType { get; } = ParameterType;
    public string DefaultExpression { get; } = DefaultExpression;
    public string MethodNamePattern { get; } = MethodNamePattern;
    public string? MethodNameReplace { get; } = MethodNameReplace;
}
