using System.Diagnostics.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Attributes;

[SuppressMessage("ReSharper", "RedundantNameQualifier")]
public enum ReturnGenerationStrategy
{
    /// <summary>
    /// Ensure the returned type on non-try methods exactly matches that of the Try-methods out parameter "value".
    /// </summary>
    Verbatim = 0,
    
    /// <summary>
    /// The non-try variant of the Try method will assume that a "true" value indicates a non-null "value". The generated method will therefore not have any nullability associated.
    /// </summary>
    TrueMeansNotNull = 1
}