using System;
using System.Diagnostics;

namespace MBW.Generators.OverloadGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
[Conditional("NEVER_RENDER")]
public sealed class DefaultOverloadAttribute(string parameterName, string defaultExpression) : Attribute
{
    public string ParameterName { get; } = parameterName;
    public string DefaultExpression { get; } = defaultExpression;
    public string MethodNameMatch { get; set; } = "^(.*)$";
    public string? MethodNameReplace { get; set; }
}