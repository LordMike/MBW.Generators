using System;
using System.Diagnostics;

namespace MBW.Generators.OverloadGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
[Conditional("NEVER_RENDER")]
public sealed class DefaultOverloadAttribute : Attribute
{
    public DefaultOverloadAttribute(string parameter, string defaultExpression)
    {
        Parameter = parameter;
        DefaultExpression = defaultExpression;
    }

    public string Parameter { get; }
    public string DefaultExpression { get; }
    public string[]? Usings { get; set; }
}