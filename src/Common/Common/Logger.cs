using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Text;

namespace MBW.Generators.Common;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers")]
internal static class Logger
{
    private static readonly Process Proc = Process.GetCurrentProcess();
    private static NamedPipeClientStream? Pipe;

    public static void Configure(bool enable, string pipeName)
    {
        if (!enable)
        {
            Pipe = null;
            return;
        }

        try
        {
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            client.Connect(50); // fail fast; don't hang the compiler
            Pipe = client;
        }
        catch
        {
            Pipe = null;
        }
    }

    internal static void Log(string message) => TrySend($"{Proc.Id}: {message}");

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
            var p = Pipe;
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