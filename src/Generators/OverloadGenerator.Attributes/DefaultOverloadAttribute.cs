using System;
using System.Diagnostics;

namespace MBW.Generators.OverloadGenerator.Attributes;

/// <summary>
/// Generates an additional overload for matching methods by supplying a default
/// value for a particular parameter.
/// </summary>
/// <param name="parameterNameRegex">Regular expression used to identify the
/// target parameter.</param>
/// <param name="defaultExpression">Expression substituted for the matched
/// parameter in the generated overload.</param>
[PublicAPI]
[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    AllowMultiple = true, Inherited = false)]
[Conditional("NEVER_RENDER")]
public sealed class DefaultOverloadAttribute([RegexPattern] string parameterNameRegex, string defaultExpression) : Attribute
{
    /// <summary>
    /// Gets the regular expression that selects which parameter to replace.
    /// </summary>
    [RegexPattern]
    public string ParameterNameRegex { get; } = parameterNameRegex;

    /// <summary>
    /// Optionally restricts matches to parameters of this exact type.
    /// </summary>
    public Type? ParameterType { get; set; }

    /// <summary>
    /// Gets the expression inserted for the parameter in the generated overload.
    ///
    /// Examples can be constants, like "String", 4, true, or expressions that can be resolved at runtime, such as "MyStaticType.Property".
    /// </summary>
    /// <remarks>Note that there is little validation on the syntax, and that it is emitted verbatim in the generated source. Use fully qualified names for types, as there are few usings available.</remarks>
    public string DefaultExpression { get; } = defaultExpression;

    /// <summary>
    /// Regular expression used to shape the generated overload name. By default, this matches the whole method name, and produces a new method named exactly the same (but with different parameters).
    /// The capture groups in this regex are available to be used in the <see cref="MethodNameReplace"/> property.
    /// </summary>
    [RegexPattern]
    public string MethodNameRegex { get; set; } = "^(.*)$";

    /// <summary>
    /// Replacement pattern applied to matched method names when emitting the overload. Capture groups from the <see cref="MethodNameRegex"/> are available here as "$1", "$2" and so on.
    /// </summary>
    public string? MethodNameReplace { get; set; }
}
