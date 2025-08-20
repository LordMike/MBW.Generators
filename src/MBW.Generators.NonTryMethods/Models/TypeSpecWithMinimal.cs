using MBW.Generators.Common.Models;

namespace MBW.Generators.NonTryMethods.Models;

internal sealed record TypeSpecWithMinimal(TypeSpec TypeSpec, MinimalStringInfo MinimalStringInfo)
{
    public TypeSpec TypeSpec { get; } = TypeSpec;
    public MinimalStringInfo MinimalStringInfo { get; } = MinimalStringInfo;
    
    public bool Equals(TypeSpecWithMinimal? other)
    {
        if (other is null)
            return false;
        return TypeSpec.Key == other.TypeSpec.Key;
    }

    public override int GetHashCode()
    {
        return TypeSpec.Key;
    }
}