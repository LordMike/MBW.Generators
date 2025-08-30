using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.GeneratorHelpers.Generator.Models;

internal readonly record struct TypeToGenerate(
    INamedTypeSymbol Type,
    string ClassName,
    string Namespace,
    Accessibility Accessibility,
    FieldToGenerate[] Fields,
    List<Diagnostic> Diagnostics,
    Location TypeLocation)
{
    public bool Equals(TypeToGenerate other)
    {
        return ClassName == other.ClassName && Namespace == other.Namespace && Accessibility == other.Accessibility && Fields.Equals(other.Fields) && Diagnostics.Equals(other.Diagnostics) && TypeLocation.Equals(other.TypeLocation);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ClassName.GetHashCode();
            hashCode = (hashCode * 397) ^ Namespace.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Accessibility;
            hashCode = (hashCode * 397) ^ Fields.GetHashCode();
            hashCode = (hashCode * 397) ^ Diagnostics.GetHashCode();
            hashCode = (hashCode * 397) ^ TypeLocation.GetHashCode();
            return hashCode;
        }
    }
}