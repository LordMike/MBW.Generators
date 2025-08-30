namespace MBW.Generators.OverloadGenerator.Attributes;

/// <summary>
/// Specifies the nullability annotation applied to a transformed parameter
/// type.
/// </summary>
[PublicAPI]
public enum TypeNullability
{
    /// <summary>
    /// The generated parameter type is non-nullable.
    /// </summary>
    NotNullable,

    /// <summary>
    /// The generated parameter type is annotated as nullable.
    /// </summary>
    Nullable
}

