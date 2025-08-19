using Microsoft.CodeAnalysis;

namespace MBW.Generators.Common.Models;

public readonly struct MinimalStringInfo(SemanticModel semanticModel, int position)
{
    public readonly SemanticModel SemanticModel = semanticModel;
    public readonly int Position = position;
}