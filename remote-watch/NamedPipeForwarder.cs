using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using HotReload.Common;
using Nerdbank.Streams;

internal static class NamedPipeForwarder
{
    public static void Start()
    {
        string pipeName = Guid.NewGuid().ToString();
        Environment.SetEnvironmentVariable(Constants.RemoteWatch.NamedPipeName, pipeName);

        int dotnetHotReloadPort = Environment.GetEnvironmentVariable(Constants.DotNet.HotReloadPort) is string port
            ? int.Parse(port)
            : 3000;
        Logger.Log($"Listening on tcp://0.0.0.0:{dotnetHotReloadPort} ({Constants.DotNet.HotReloadPort})...");
        var listener = new TcpListener(IPAddress.Any, dotnetHotReloadPort);
        listener.Start();
        Logger.Log(
            $"Waiting for connection from port {dotnetHotReloadPort}, please launch {Constants.HotReloadClient}."
        );

        _ = Task.Run(async () =>
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                Logger.Log($"Accepted a connection from port {dotnetHotReloadPort}.");
                await using var multiplexer = await MultiplexingStream.CreateAsync(
                    stream,
                    new MultiplexingStream.Options { ProtocolMajorVersion = 3 }
                );
                using var pidChannel = await multiplexer.OfferChannelAsync(Constants.RemoteWatch.PidChannel);
                using var healthChannel = await multiplexer.OfferChannelAsync(Constants.RemoteWatch.HealthChannel);

                while (true)
                {
                    using var pipe = NamedPipe.CreateServer(pipeName);
                    await pipe.WaitForConnectionAsync();
                    Logger.Log("Accepted a connection from remote watch named pipe.");

                    int pid = await PipeReader.Create(pipe).ReadIntAsync();
                    Logger.Log($"Received PID: {pid}");
                    using var process = Process.GetProcessById(pid);

                    await pidChannel.Output.WriteIntAsync(pid);

                    using var channel = await multiplexer.OfferChannelAsync(pid.ToString());
                    await Task.WhenAny(
                        StreamForwarder.ForwardAsync(pipe, channel.AsStream()),
                        process.WaitForExitAsync()
                    );

                    try
                    {
                        await healthChannel.Output.WriteIntAsync(0);
                        await healthChannel.Input.ReadIntAsync();
                    }
                    catch (Exception)
                    {
                        Logger.Log("Client disconnected, exiting.");
                        Environment.Exit(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                Environment.Exit(0);
            }
        });
    }
}
