using Microsoft.CodeAnalysis;

namespace MBW.Generators.Common.Models;

internal readonly struct MinimalStringInfo(SemanticModel semanticModel, int position)
{
    internal readonly SemanticModel SemanticModel = semanticModel;
    internal readonly int Position = position;
}