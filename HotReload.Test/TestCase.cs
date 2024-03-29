using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using HotReload.Common;
using Microsoft.DotNet.Cli.Utils;
using Constants = HotReload.Common.Constants;

internal static class TestCase
{
    public const int PingPort = 6001;
    public const int PidPort = 6002;

    private static string GetFilePath([CallerFilePath] string? path = null) => path!;

    private static void Run(string command, IEnumerable<string> args)
    {
        Console.WriteLine($"{command} {ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args)}");
        var result = Command.Create(command, args).Execute();
        if (result.ExitCode != 0)
        {
            throw new Win32Exception(result.ExitCode);
        }
    }

    private static Process LaunchProcess(ProcessStartInfo startInfo)
    {
        var process = new Process { StartInfo = startInfo };
        AppDomain.CurrentDomain.ProcessExit += (_, _) => process.KillTree();
        process.Start();
        return process;
    }

    public static async ValueTask ReceivePingAsync(TcpListener listener, byte expectedValue, CancellationToken token)
    {
        var buffer = new byte[1];
        while (true)
        {
            using var client = await listener.AcceptTcpClientAsync(token);
            using var stream = client.GetStream();
            await stream.ReadExactlyAsync(buffer, token);
            Console.WriteLine($"Received: {buffer[0]}");
            if (buffer[0] == expectedValue)
            {
                break;
            }
        }
    }

    public delegate ValueTask Verify(
        string testProjectPath,
        Process watchServer,
        Process watchClient,
        TcpListener pingListener,
        CancellationToken token
    );

    public static async Task RunAsync(string message, Verify verify)
    {
        Console.WriteLine($"Test case: {message}");

        string projectPath = Path.GetDirectoryName(GetFilePath())!;
        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        try
        {
            Run("cp", ["--recursive", Path.Combine(projectPath, "TestProject"), tempPath]);
            string testProjectPath = Path.Combine(tempPath, "TestProject");
            string hotReloadClientPath = Path.Combine(testProjectPath, "Bin/HotReload.Client");

            using var pingListener = new TcpListener(IPAddress.Loopback, 6001);
            pingListener.Start();
            Environment.SetEnvironmentVariable("HOTRELOAD_TESTING", "1");
            Environment.SetEnvironmentVariable(Constants.DotNet.HotReloadPort, "6000");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var watchServer = LaunchProcess(
                new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "remote-watch --non-interactive",
                    WorkingDirectory = testProjectPath,
                    UseShellExecute = false,
                }
            );

            async Task<Process> LaunchClientAsync()
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                    try
                    {
                        return LaunchProcess(
                            new ProcessStartInfo { FileName = hotReloadClientPath, UseShellExecute = false, }
                        );
                    }
                    catch (Win32Exception ex)
                    {
                        if (ex.NativeErrorCode is not 2 and not 26)
                        {
                            Console.WriteLine(ex.NativeErrorCode);
                            await Console.Error.WriteLineAsync(ex.ToString());
                        }
                    }
                }
            }
            using var watchClient = await LaunchClientAsync();

            await verify(testProjectPath, watchServer, watchClient, pingListener, cts.Token);

            watchServer.KillTree();
            await watchServer.WaitForExitAsync(cts.Token);
            await watchClient.WaitForExitAsync(cts.Token);
        }
        finally
        {
            Directory.Delete(tempPath, recursive: true);
            Console.WriteLine();
        }
    }
}
