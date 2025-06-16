using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

const int pingPort = 6001;
const int pidPort = 6002;

const byte value = 1;

_ = Task.Run(async () =>
{
    using var client = new TcpClient();
    await client.ConnectAsync(IPAddress.Loopback, pidPort);
    using var stream = client.GetStream();

    var buffer = new byte[sizeof(int)];
    BinaryPrimitives.WriteInt32LittleEndian(buffer, Environment.ProcessId);
    await stream.WriteAsync(buffer);
});

static async Task PingAsync()
{
    Console.WriteLine($"Sending ping with value: {value}...");
    using var client = new TcpClient();
    await client.ConnectAsync(IPAddress.Loopback, pingPort);
    using var stream = client.GetStream();
    stream.WriteByte(value);
    await stream.FlushAsync();
}

{
    while (true)
    {
        await PingAsync();
        await Task.Delay(TimeSpan.FromSeconds(3));
    }
}
