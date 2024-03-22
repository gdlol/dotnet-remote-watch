using System.IO.Pipes;

namespace HotReload.Common;

public static class NamedPipe
{
    public static NamedPipeServerStream CreateServer(string pipeName)
    {
        return new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous
        );
    }

    public static NamedPipeClientStream CreateClient(string pipeName)
    {
        return new NamedPipeClientStream(serverName: ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
    }
}
