using System;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.Common;

internal delegate void GeneratorCommonInitialize(ref IncrementalGeneratorInitializationContext context);

internal static class GeneratorCommon
{
    public static void Initialize<TGenerator>(ref IncrementalGeneratorInitializationContext context,
        GeneratorCommonInitialize @delegate)
    {
        // Configure logger, when options change
        var logOptionProvider = context.AnalyzerConfigOptionsProvider.Select((provider, _) =>
        {
            bool enabled = false;
            if (provider.GlobalOptions.TryGetValue("mbw_generators_logging_enabled", out var enabledStr) &&
                bool.TryParse(enabledStr, out var newEnabled))
                enabled = newEnabled;

            string pipeName = "MBW.Generators.Log";
            if (provider.GlobalOptions.TryGetValue("mbw_generators_logging_pipeName", out var newPipeName))
                pipeName = newPipeName;

            return (enabled, pipeName);
        });

        context.RegisterSourceOutput(logOptionProvider,
            (_, tuple) => { Logger.Configure(tuple.enabled, tuple.pipeName); });

        context.RegisterSourceOutput(context.CompilationProvider, (_, _) => { Logger.Log("## Compilation run"); });

        try
        {
            Logger.Log(string.Empty);
            Logger.Log("Initializing " + typeof(TGenerator).Name);
            @delegate(ref context);
            Logger.Log("Initialized " + typeof(TGenerator).Name);
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