using System;
using System.Diagnostics;

namespace MBW.Generators.OverloadGenerator.Attributes;

[PublicAPI]
[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    AllowMultiple = true, Inherited = false)]
[Conditional("NEVER_RENDER")]
public sealed class DefaultOverloadAttribute([RegexPattern] string parameterNameRegex, string defaultExpression) : Attribute
{
    [RegexPattern] public string ParameterNameRegex { get; } = parameterNameRegex;
    public Type? ParameterType { get; set; }
    public string DefaultExpression { get; } = defaultExpression;
    [RegexPattern] public string MethodNameRegex { get; set; } = "^(.*)$";
    public string? MethodNameReplace { get; set; }
}