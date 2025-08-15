using System;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed class GenerateNonTryMethodAttributeInfo(
    Location location,
    ITypeSymbol? exceptionType,
    string methodNamePattern)
{
    public Location Location { get; } = location;
    public ITypeSymbol? ExceptionType { get; } = exceptionType;
    public string MethodNamePattern { get; } = methodNamePattern;
}