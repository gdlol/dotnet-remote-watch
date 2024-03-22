using System.IO.Pipelines;
using HotReload.Common;

// Obtains target assembly name when building client, and later forward to StartupHook.
internal class TargetAssemblyName
{
    // To be called from remote-watch.
    public static void Listen()
    {
        string pipeName = Guid.NewGuid().ToString();
        Environment.SetEnvironmentVariable(Constants.RemoteWatch.TargetAssemblyNamedPipeName, pipeName);

        Logger.Log("Listening for target assembly name...");
        Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    string targetAssemblyName;
                    {
                        using var pipe = NamedPipe.CreateServer(pipeName);
                        await pipe.WaitForConnectionAsync();
                        var reader = PipeReader.Create(pipe);
                        targetAssemblyName = await reader.ReadStringAsync();
                    }
                    Logger.Log($"Target assembly name: {targetAssemblyName}.");

                    {
                        using var pipe = NamedPipe.CreateServer(pipeName);
                        await pipe.WaitForConnectionAsync();
                        var writer = PipeWriter.Create(pipe);
                        await writer.WriteStringAsync(targetAssemblyName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        });
    }

    // To be called when building client.
    public static async Task SendAsync(string targetAssemblyName)
    {
        string pipeName = EnvironmentVariable.Load(Constants.RemoteWatch.TargetAssemblyNamedPipeName);

        using var pipe = NamedPipe.CreateClient(pipeName);
        await pipe.ConnectAsync();
        var writer = PipeWriter.Create(pipe);
        await writer.WriteStringAsync(targetAssemblyName);
    }

    // To be called by StartupHook.
    public static async Task<string> ReceiveAsync()
    {
        string pipeName = EnvironmentVariable.Load(Constants.RemoteWatch.TargetAssemblyNamedPipeName);

        using var pipe = NamedPipe.CreateClient(pipeName);
        await pipe.ConnectAsync();
        var reader = PipeReader.Create(pipe);
        return await reader.ReadStringAsync();
    }
}
