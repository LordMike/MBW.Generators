using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace MBW.Generators.Common;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers")]
internal static class Logger
{
    private const string PipeName = "MBW.GeneratorsLogPipe";
    private static readonly Process Proc = Process.GetCurrentProcess();

    // Lazy connect; if it fails once we stay "disabled".
    private static readonly Lazy<NamedPipeClientStream?> Pipe = new(() =>
    {
        try
        {
            var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(50); // fail fast; don't hang the compiler
            return client;
        }
        catch
        {
            return null;
        }
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    [Conditional("ENABLE_PIPE_LOGGING")]
    internal static void Log(string message) => TrySend($"{Proc.Id}: {message}");

    [Conditional("ENABLE_PIPE_LOGGING")]
    internal static void Log(Exception e, string message = "An error occurred") =>
        TrySend($"""
                 {Proc.Id}: {message}
                 Error:   {e.GetType().FullName}
                 Message: {e.Message}
                 Stack:
                 {e.StackTrace}
                 """);

    private static void TrySend(string payload)
    {
        try
        {
            var p = Pipe.Value;
            if (p is null)
                return;

            // One write == one message boundary in Message mode.
            var bytes = Encoding.UTF8.GetBytes(payload);
            p.Write(bytes, 0, bytes.Length);
            p.Flush();
        }
        catch
        {
            // Swallow all logging failures unconditionally.
        }
    }
}