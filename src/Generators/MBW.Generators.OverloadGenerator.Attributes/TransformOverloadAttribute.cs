using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace MBW.Generators.OverloadGenerator.Attributes;

[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    AllowMultiple = true, Inherited = false)]
[Conditional("NEVER_RENDER")]
public sealed class TransformOverloadAttribute(
    [RegexPattern] string parameterNameRegex,
    Type newType,
    string transformExpression = "{value}.ToString()",
    TypeNullability newTypeNullability = TypeNullability.NotNullable)
    : Attribute
{
    [RegexPattern] public string ParameterNameRegex { get; } = parameterNameRegex;
    public Type? ParameterType { get; set; }
    public Type NewType { get; } = newType;
    public string TransformExpression { get; } = transformExpression;
    public TypeNullability NewTypeNullability { get; } = newTypeNullability;
    [RegexPattern] public string MethodNameRegex { get; set; } = "^(.*)$";
    public string? MethodNameReplace { get; set; }
}