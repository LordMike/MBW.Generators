using System.Text.RegularExpressions;
using MBW.Generators.OverloadGenerator.Attributes;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Generator.Models;

internal readonly record struct TransformOverloadAttributeInfoWithRegex(
    TransformOverloadAttributeInfo Info,
    Regex ParameterNameRegex,
    Regex MethodNameRegex)
{
    public Location Location => Info.Location;
    public INamedTypeSymbol? ParameterType => Info.ParameterType;
    public INamedTypeSymbol NewType => Info.NewType;
    public string TransformExpression => Info.TransformExpression;
    public TypeNullability NewTypeNullability => Info.NewTypeNullability;
    public string? MethodNameReplace => Info.MethodNameReplace;
    public Regex ParameterNamePattern => ParameterNameRegex;
    public Regex MethodNamePattern => MethodNameRegex;
}
