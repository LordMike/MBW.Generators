using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers")]
internal static class Logger
{
    private static readonly Process Proc = Process.GetCurrentProcess();

    // lazy init, but swallow failures
    private static readonly Lazy<StreamWriter?> LogDestination = new(() =>
    {
        try
        {
            var client = new NamedPipeClientStream(".", "MBW.SourcegenLogger", PipeDirection.Out);
            client.Connect(50); // fail fast (don’t hang the compiler)

            var sw = new StreamWriter(client, Encoding.UTF8) { AutoFlush = true };
            return sw;
        }
        catch
        {
            // Could not connect — disable logging
            return null;
        }
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    internal static void Log(string message)
    {
        try
        {
            var sw = LogDestination.Value;
            if (sw is null)
                return;

            var line = $"{Proc.Id} - {DateTime.Now:HHmmss}:  {message}";
            sw.WriteLine(line);
        }
        catch
        {
            // never propagate log failures into the generator
        }
    }

    internal static void Log(Exception e, string message = "An error occurred")
    {
        try
        {
            var sw = LogDestination.Value;
            if (sw is null)
                return;

            var line = $"""
                        {Proc.Id} - {DateTime.Now:HHmmss}:  {message}
                        Error:   {e.GetType().FullName}
                        Message: {e.Message}
                        Stack:
                        {e.StackTrace}

                        """;
            sw.WriteLine(line);
        }
        catch
        {
            // ignore
        }
    }
}