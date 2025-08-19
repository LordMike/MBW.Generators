using Microsoft.CodeAnalysis;

namespace MBW.Generators.Common;

public abstract class GeneratorBase : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        InitializeInternal(context);
    }

    protected abstract void InitializeInternal(IncrementalGeneratorInitializationContext context);
}