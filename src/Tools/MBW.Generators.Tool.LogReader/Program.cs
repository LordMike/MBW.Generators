using System.IO.Pipes;
using System.Text;

namespace MBW.Generators.MBW.Generators.Tool.LogReader;

static class Program
{
    const string PipeName = "MBW.GeneratorsLogPipe";

    static async Task Main()
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Log($"Starting listener on pipe '{PipeName}' (Ctrl+C to stop)…");

        try
        {
            while (!cts.IsCancellationRequested)
            {
                var server = CreateServer();
                try
                {
                    await server.WaitForConnectionAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    server.Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    server.Dispose();
                    Log($"Error waiting for connection: {ex.Message}");
                    continue;
                }

                var clientId = Interlocked.Increment(ref _nextClientId);
                string? user = null;
                try
                {
                    user = server.GetImpersonationUserName();
                }
                catch
                {
                    /* non-Windows or not available */
                }

                Log($"Client #{clientId} connected{(string.IsNullOrEmpty(user) ? "" : $" as '{user}'")}.");

                _ = Task.Run(() => HandleClientAsync(server, clientId, cts.Token));
            }
        }
        finally
        {
            Log("Listener stopping.");
        }
    }

    static NamedPipeServerStream CreateServer() =>
        new(PipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Message, PipeOptions.Asynchronous);

    static async Task HandleClientAsync(NamedPipeServerStream server, int clientId, CancellationToken ct)
    {
        using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096, leaveOpen: false);
        var sb = new StringBuilder();
        var buf = new char[4096];

        try
        {
            while (!ct.IsCancellationRequested && server.IsConnected)
            {
                var read = await reader.ReadAsync(buf.AsMemory(0, buf.Length), ct); // let exceptions bubble
                if (read == 0) break; // graceful disconnect

                sb.Append(buf, 0, read);

                if (server.IsMessageComplete)
                {
                    Log($"#{clientId}: {sb}");
                    sb.Clear();
                }
            }

            Log($"Client #{clientId} disconnected.");
        }
        catch (Exception ex)
        {
            Log($"Client #{clientId} disconnected with error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    static int _nextClientId;

    static void Log(string msg) =>
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
}