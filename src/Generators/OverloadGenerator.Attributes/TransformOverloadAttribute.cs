using System;
using System.Diagnostics;

namespace MBW.Generators.OverloadGenerator.Attributes;

/// <summary>
/// Generates an additional overload for matching methods by changing the type of
/// a parameter and applying a transformation when invoking the original method.
/// </summary>
/// <param name="parameterNameRegex">Regular expression used to identify the
/// parameter to transform.</param>
/// <param name="newType">The type to use for the parameter in the generated
/// overload.</param>
/// <param name="transformExpression">Expression converting the new parameter
/// value to the original type. Use <c>{value}</c> to reference the provided
/// argument.</param>
/// <param name="newTypeNullability">Indicates whether the generated parameter
/// should be declared as nullable.</param>
[PublicAPI]
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
    /// <summary>
    /// Gets the regular expression that selects which parameter to transform.
    /// </summary>
    [RegexPattern]
    public string ParameterNameRegex { get; } = parameterNameRegex;

    /// <summary>
    /// Optionally restricts matches to parameters of this type.
    /// </summary>
    public Type? ParameterType { get; set; }

    /// <summary>
    /// Gets the new type used for the parameter in the generated overload.
    /// </summary>
    public Type NewType { get; } = newType;

    /// <summary>
    /// Gets the expression inserted for the parameter in the generated overload. This will replace "{value}" with the new arguments value.
    ///
    /// An example to go from a new enum-based overload, to a string parameter, is to write: "{value}.ToString()".
    /// </summary>
    /// <remarks>Note that there is little validation on the syntax, and that it is emitted verbatim in the generated source. Use fully qualified names for types, as there are few usings available.</remarks>
    public string TransformExpression { get; } = transformExpression;

    /// <summary>
    /// Gets the nullability annotation applied to the new parameter type.
    /// </summary>
    public TypeNullability NewTypeNullability { get; } = newTypeNullability;

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
