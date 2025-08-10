using System;

namespace MBW.Generators.OverloadGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class TransformOverloadAttribute : Attribute
{
    public TransformOverloadAttribute(string parameter, Type accept, string transform)
    {
        Parameter = parameter;
        Accept = accept;
        Transform = transform;
    }

    public string Parameter { get; }
    public Type Accept { get; }
    public string Transform { get; }
    public string[]? Usings { get; set; }
}