using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed class GenerateNonTryMethodAttributeInfo(ITypeSymbol? exceptionType, string methodNamePattern)
{
    public ITypeSymbol? ExceptionType { get; } = exceptionType;
    public string MethodNamePattern { get; } = methodNamePattern;
    public Regex Pattern { get; } = new(methodNamePattern);
}