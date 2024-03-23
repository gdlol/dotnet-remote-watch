namespace HotReload.Common;

public static class StreamForwarder
{
    public static async Task ForwardAsync(Stream stream1, Stream stream2)
    {
        using var cts = new CancellationTokenSource();

        var task1 = stream1.CopyToAsync(stream2, cts.Token);
        var task2 = stream2.CopyToAsync(stream1, cts.Token);
        await Task.WhenAny(task1, task2);
        await cts.CancelAsync();
        try
        {
            await Task.WhenAll(task1, task2);
        }
        catch (OperationCanceledException) { }
    }
}
