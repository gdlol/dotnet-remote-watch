using System.IO.Pipelines;
using System.Reflection;
using HotReload.Common;

internal class StartupHook
{
    private static bool initialized = false;

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }
        if (Assembly.GetEntryAssembly() != Assembly.GetExecutingAssembly())
        {
            return;
        }
        if (Environment.GetEnvironmentVariable(Constants.RemoteWatch.BuildClient) == "1")
        {
            return;
        }

        // Intercepts the named pipe so that Microsoft.Extensions.DotNetDeltaApplier.dll does nothing on the server side.
        string hotReloadNamedPipeName = EnvironmentVariable.Load(Constants.DotNet.HotReloadNamedPipeName);
        Environment.SetEnvironmentVariable(Constants.DotNet.HotReloadNamedPipeName, null);

        string remoteWatchNamedPipeName = EnvironmentVariable.Load(Constants.RemoteWatch.NamedPipeName);

        // Cleanup startup hook environment variable.
        string selfPath = Assembly.GetExecutingAssembly().Location;
        var hooks =
            Environment
                .GetEnvironmentVariable(Constants.DotNet.StartupHooks)
                ?.Split(Path.PathSeparator)
                .Where(s => s != selfPath) ?? [];
        string deltaApplierPath =
            hooks.FirstOrDefault(s => s.EndsWith(Constants.DeltaApplierDllName))
            ?? throw new InvalidOperationException(
                $"{Constants.DeltaApplierDllName} not found in {Constants.DotNet.StartupHooks}."
            );
        Environment.SetEnvironmentVariable(Constants.DotNet.StartupHooks, string.Join(Path.PathSeparator, hooks));

        _ = Task.Run(async () =>
        {
            try
            {
                using var hotReloadNamedPipe = NamedPipe.CreateClient(hotReloadNamedPipeName);
                await hotReloadNamedPipe.ConnectAsync();

                using var remoteWatchNamedPipe = NamedPipe.CreateClient(remoteWatchNamedPipeName);
                await remoteWatchNamedPipe.ConnectAsync();

                // Wait ready.
                var writer = PipeWriter.Create(remoteWatchNamedPipe);

                Logger.Log($"Sending PID ({Environment.ProcessId}) to remote watch named pipe...");
                await writer.WriteIntAsync(Environment.ProcessId);

                Logger.Log($"Sending {Constants.DeltaApplierDllName} to client...");
                {
                    var fileInfo = new FileInfo(deltaApplierPath);
                    using var file = fileInfo.OpenRead();

                    await writer.WriteLongAsync(fileInfo.Length);
                    await file.CopyToAsync(writer);
                }

                Logger.Log("Receiving target assembly name...");
                string targetAssemblyName = await TargetAssemblyName.ReceiveAsync();

                Logger.Log($"Sending target assembly name ({targetAssemblyName}) to client...");
                await writer.WriteStringAsync(targetAssemblyName);

                Logger.Log($"Forwarding hot reload named pipe to client...");
                await StreamForwarder.ForwardAsync(hotReloadNamedPipe, remoteWatchNamedPipe);

                Logger.Log("Client disconnected.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
            }
        });

        initialized = true;
    }
}
