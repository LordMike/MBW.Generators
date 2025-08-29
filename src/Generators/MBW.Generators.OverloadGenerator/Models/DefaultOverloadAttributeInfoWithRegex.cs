using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Models;

internal readonly record struct DefaultOverloadAttributeInfoWithRegex(
    DefaultOverloadAttributeInfo Info,
    Regex ParameterNameRegex,
    Regex MethodNameRegex)
{
    public Location Location => Info.Location;
    public INamedTypeSymbol? ParameterType => Info.ParameterType;
    public string DefaultExpression => Info.DefaultExpression;
    public string? MethodNameReplace => Info.MethodNameReplace;
    public Regex ParameterNamePattern => ParameterNameRegex;
    public Regex MethodNamePattern => MethodNameRegex;
}
