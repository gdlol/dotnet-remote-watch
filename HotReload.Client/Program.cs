using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using HotReload.Common;

int hotReloadPort = Environment.GetEnvironmentVariable(Constants.DotNet.HotReloadPort) is string port
    ? int.Parse(port)
    : 3000;

while (true)
{
    Console.WriteLine($"Connecting to {Constants.DotNet.HotReloadPort} ({hotReloadPort})...");
    using var client = new TcpClient();
    await client.ConnectAsync(IPAddress.Loopback, 3000);
    Console.WriteLine("Connected.");
    using var stream = client.GetStream();
    var reader = PipeReader.Create(stream);

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

    async Task ForwardHotReloadStream(string pipeName)
    {
        try
        {
            using var pipe = NamedPipe.CreateServer(pipeName);
            Console.WriteLine("Waiting for named pipe connection from App...");
            await pipe.WaitForConnectionAsync();
            Console.WriteLine("Accepted named pipe connection from App.");

            await StreamForwarder.ForwardAsync(stream, pipe);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    var forward = ForwardHotReloadStream(pipeName);

    string fileName =
        Path.Combine(AppContext.BaseDirectory, targetAssemblyName) + Path.GetExtension(Environment.ProcessPath);
    Console.WriteLine($"Starting {fileName}...");
    using var process = new Process();
    process.StartInfo.FileName = fileName;
    process.StartInfo.UseShellExecute = true;
    process.Start();

    await process.WaitForExitAsync();
    Console.WriteLine("App exited.");
    await forward;
    Console.WriteLine();
}
