using System;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.Common;

internal delegate void GeneratorCommonInitialize(ref IncrementalGeneratorInitializationContext context);

internal static class GeneratorCommon
{
    public static void Initialize<TGenerator>(ref IncrementalGeneratorInitializationContext context, GeneratorCommonInitialize @delegate)
    {
        try
        {
            Logger.Log(string.Empty);
            Logger.Log("Initializing");
            @delegate(ref context);
            Logger.Log("Initialized");
        }
        catch (Exception e)
        {
            Logger.Log(e, "Initialization failed");

            context.RegisterSourceOutput(context.CompilationProvider,
                (productionContext, _) =>
                {
                    // "The {0} Generator encountered an issue when generating, exception: {1}, message: {2}, Stack: {3}",
                    productionContext.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.ExceptionError,
                        Location.None,
                        typeof(TGenerator).Name, e.GetType().Name, e.Message, e.StackTrace));
                });
        }
    }
}