using System;
using System.Diagnostics;

namespace MBW.Generators.OverloadGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
[Conditional("NEVER_RENDER")]
public sealed class TransformOverloadAttribute(
    string parameterName,
    Type newType,
    string transformExpression = "{value}.ToString()",
    TypeNullability newTypeNullability = TypeNullability.NotNullable)
    : Attribute
{
    public string ParameterName { get; } = parameterName;
    public Type? ParameterType { get; set; }
    public Type NewType { get; } = newType;
    public string TransformExpression { get; } = transformExpression;
    public TypeNullability NewTypeNullability { get; } = newTypeNullability;

    public string MethodNameMatch { get; set; } = "^(.*)$";
    public string? MethodNameReplace { get; set; }
}