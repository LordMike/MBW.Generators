using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed record GenerateNonTryMethodAttributeInfoWithValidPattern(
    GenerateNonTryMethodAttributeInfo info,
    INamedTypeSymbol exceptionType,
    Regex methodNamePatternRegex)
{
    public Location Location { get; } = info.Location;

    public INamedTypeSymbol ExceptionType { get; } = exceptionType;

    public string MethodNamePattern { get; } = info.MethodNamePattern;
    public Regex Pattern { get; } = methodNamePatternRegex;
}