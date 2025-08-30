using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Generator.Models;

internal sealed record GenerateNonTryMethodAttributeInfoWithValidPattern(
    GenerateNonTryMethodAttributeInfo info,
    INamedTypeSymbol exceptionType,
    Regex methodNamePatternRegex)
{
    public INamedTypeSymbol ExceptionType { get; } = exceptionType;

    public string MethodNamePattern { get; } = info.MethodNamePattern;
    public Regex Pattern { get; } = methodNamePatternRegex;
}