using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using HotReload.Common;
using Nerdbank.Streams;

int hotReloadPort = Environment.GetEnvironmentVariable(Constants.DotNet.HotReloadPort) is string port
    ? int.Parse(port)
    : 3000;
Console.WriteLine($"Connecting to {Constants.DotNet.HotReloadPort} ({hotReloadPort})...");
using var client = new TcpClient();
await client.ConnectAsync(IPAddress.Loopback, 3000);
Console.WriteLine("Connected.");
using var stream = client.GetStream();
await using var multiplexer = await MultiplexingStream.CreateAsync(
    stream,
    new MultiplexingStream.Options { ProtocolMajorVersion = 3 }
);
using var pidChannel = await multiplexer.AcceptChannelAsync(Constants.RemoteWatch.PidChannel);
using var healthChannel = await multiplexer.AcceptChannelAsync(Constants.RemoteWatch.HealthChannel);

_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            await healthChannel.Input.ReadIntAsync();
            await healthChannel.Output.WriteIntAsync(0);
        }
        catch (Exception)
        {
            Console.WriteLine("Disconnected from server, exiting.");
            Environment.Exit(0);
        }
    }
});

while (true)
{
    try
    {
        int pid = await pidChannel.Input.ReadIntAsync();
        Console.WriteLine($"Received PID: {pid}");

        using var channel = await multiplexer.AcceptChannelAsync(pid.ToString());
        var reader = channel.Input;

        Console.WriteLine($"Receiving {Constants.DeltaApplierDllName}...");
        {
            long length = await reader.ReadLongAsync();

            using var file = File.OpenWrite(Constants.DeltaApplierDllName);
            var writer = PipeWriter.Create(file);
            await reader.ReadBytesAsync(writer, length);
        }

        Console.WriteLine($"Receiving target assembly name...");
        string targetAssemblyName = await reader.ReadStringAsync();
        Console.WriteLine($"Received target assembly name: {targetAssemblyName}");

        Environment.SetEnvironmentVariable(
            Constants.DotNet.StartupHooks,
            Path.Combine(AppContext.BaseDirectory, Constants.DeltaApplierDllName)
        );
        string pipeName = Guid.NewGuid().ToString();
        Environment.SetEnvironmentVariable(Constants.DotNet.HotReloadNamedPipeName, pipeName);
        Environment.SetEnvironmentVariable(Constants.DotNet.Watch, "1");
        Environment.SetEnvironmentVariable(Constants.DotNet.ModifiableAssemblies, "debug");

        string fileName =
            Path.Combine(AppContext.BaseDirectory, targetAssemblyName) + Path.GetExtension(Environment.ProcessPath);
        Console.WriteLine($"Starting {fileName}...");
        using var process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.UseShellExecute = true;
        void cleanup(object? _, EventArgs __) => process.Kill(entireProcessTree: true);
        AppDomain.CurrentDomain.ProcessExit += cleanup;
        process.Exited += (_, _) =>
        {
            AppDomain.CurrentDomain.ProcessExit -= cleanup;
            Console.WriteLine("App exited.");
        };
        process.Start();

        using var pipe = NamedPipe.CreateServer(pipeName);
        Console.WriteLine("Waiting for named pipe connection from App...");
        await pipe.WaitForConnectionAsync();
        Console.WriteLine("Accepted named pipe connection from App.");

        await Task.WhenAny(StreamForwarder.ForwardAsync(channel.AsStream(), pipe), process.WaitForExitAsync());
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
        await process.WaitForExitAsync();

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
    }
}
