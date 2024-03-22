using System.Net;
using System.Net.Sockets;
using HotReload.Common;

internal static class NamedPipeForwarder
{
    public static void Start()
    {
        string pipeName = Guid.NewGuid().ToString();
        Environment.SetEnvironmentVariable(Constants.RemoteWatch.NamedPipeName, pipeName);

        int dotnetHotReloadPort = Environment.GetEnvironmentVariable(Constants.DotNet.HotReloadPort) is string port
            ? int.Parse(port)
            : 3000;
        Logger.Log($"Listening on {Constants.DotNet.HotReloadPort} ({dotnetHotReloadPort})...");
        var listener = new TcpListener(IPAddress.Any, dotnetHotReloadPort);
        listener.Start();

        Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    using var pipe = NamedPipe.CreateServer(pipeName);
                    await pipe.WaitForConnectionAsync();
                    Logger.Log("Accepted a connection from remote watch named pipe.");

                    Logger.Log(
                        $"Waiting for connection from port {dotnetHotReloadPort}, please launch {Constants.HotReloadClient}."
                    );
                    using var client = await listener.AcceptTcpClientAsync();
                    using var stream = client.GetStream();
                    Logger.Log($"Accepted a connection from port {dotnetHotReloadPort}.");

                    // Signal ready.
                    await pipe.WriteAsync(new byte[1]);

                    await StreamForwarder.ForwardAsync(pipe, stream);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        });
    }
}
