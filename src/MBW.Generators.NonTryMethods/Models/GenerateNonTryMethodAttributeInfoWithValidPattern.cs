using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed class GenerateNonTryMethodAttributeInfoWithValidPattern(
    GenerateNonTryMethodAttributeInfo info)
{
    public Location Location { get; } = info.Location;
    public ITypeSymbol? ExceptionType { get; } = info.ExceptionType;
    public string MethodNamePattern { get; } = info.MethodNamePattern;
    public Regex Pattern { get; } = new(info.MethodNamePattern);
}