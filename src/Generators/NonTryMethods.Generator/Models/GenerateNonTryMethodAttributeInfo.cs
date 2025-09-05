using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Generator.Models;

internal sealed class GenerateNonTryMethodAttributeInfo(
    Location location,
    ITypeSymbol? exceptionType,
    string? exceptionTypeName,
    string methodNamePattern)
{
    public Location Location { get; } = location;
    public ITypeSymbol? ExceptionType { get; } = exceptionType;
    public string? ExceptionTypeName { get; } = exceptionTypeName;
    public string MethodNamePattern { get; } = methodNamePattern;
}