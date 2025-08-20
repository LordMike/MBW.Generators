using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.Common;

public abstract class GeneratorBase<TGenerator> : IIncrementalGenerator where TGenerator : GeneratorBase<TGenerator>
{
    protected void TryExecute<TState>(Action<TGenerator, TState> action, TState state)
    {
        try
        {
            action((TGenerator)this, state);
        }
        catch (Exception e)
        {
            Logger.Log("Exception: " + $"""
                                        /*
                                        Error:   {e.GetType().FullName}
                                        Message: {e.Message}
                                        Stack:
                                        {e.StackTrace}
                                        */
                                        """);
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        try
        {
            Logger.Log("Initializing");
            InitializeInternal(context);
            Logger.Log("Initialized");
        }
        catch (Exception e)
        {
            context.RegisterPostInitializationOutput(initializationContext =>
            {
                initializationContext.AddSource("GeneratorError.g.cs",
                    $"""
                     /*
                     Error:   {e.GetType().FullName}
                     Message: {e.Message}
                     Stack:
                     {e.StackTrace}
                     */
                     """);
            });
        }
    }

    protected abstract void InitializeInternal(IncrementalGeneratorInitializationContext context);
}