using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.GenerationModels;

internal readonly struct MinimalStringInfo(SemanticModel semanticModel, int position)
{
    public readonly SemanticModel SemanticModel = semanticModel;
    public readonly int Position = position;
}