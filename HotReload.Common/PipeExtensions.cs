using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Text;

namespace HotReload.Common;

public static class PipeExtensions
{
    private static void Check(this ReadResult result)
    {
        if (result.IsCanceled)
        {
            throw new OperationCanceledException();
        }
    }

    private static void Check(this FlushResult result)
    {
        if (result.IsCanceled)
        {
            throw new OperationCanceledException();
        }
    }

    private static int ReadInt(ReadOnlySequence<byte> buffer)
    {
        var sequenceReader = new SequenceReader<byte>(buffer);
        if (!sequenceReader.TryReadLittleEndian(out int value))
        {
            throw new InvalidOperationException();
        }
        return value;
    }

    private static long ReadLong(ReadOnlySequence<byte> buffer)
    {
        var sequenceReader = new SequenceReader<byte>(buffer);
        if (!sequenceReader.TryReadLittleEndian(out long value))
        {
            throw new InvalidOperationException();
        }
        return value;
    }

    public static async ValueTask<int> ReadIntAsync(
        this PipeReader reader,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await reader.ReadAtLeastAsync(sizeof(int), cancellationToken);
        result.Check();

        int value = ReadInt(result.Buffer);
        reader.AdvanceTo(result.Buffer.GetPosition(sizeof(int)));
        return value;
    }

    public static async ValueTask<long> ReadLongAsync(
        this PipeReader reader,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await reader.ReadAtLeastAsync(sizeof(long), cancellationToken);
        result.Check();

        long value = ReadLong(result.Buffer);
        reader.AdvanceTo(result.Buffer.GetPosition(sizeof(long)));
        return value;
    }

    public static async ValueTask<string> ReadStringAsync(
        this PipeReader reader,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        int length = await reader.ReadIntAsync(cancellationToken);

        var result = await reader.ReadAtLeastAsync(length, cancellationToken);
        result.Check();

        string value = Encoding.UTF8.GetString(result.Buffer.Slice(0, length));
        reader.AdvanceTo(result.Buffer.GetPosition(length));
        return value;
    }

    public static async ValueTask ReadBytesAsync(
        this PipeReader reader,
        PipeWriter writer,
        long length,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        while (length > 0)
        {
            var result = await reader.ReadAsync(cancellationToken);
            result.Check();
            var buffer = result.Buffer;
            if (buffer.Length > length)
            {
                buffer = buffer.Slice(0, length);
            }

            if (buffer.IsSingleSegment)
            {
                var flushResult = await writer.WriteAsync(buffer.First, cancellationToken);
                flushResult.Check();
            }
            else
            {
                foreach (var segment in buffer)
                {
                    var flushResult = await writer.WriteAsync(segment, cancellationToken);
                    flushResult.Check();
                }
            }

            length -= buffer.Length;
            reader.AdvanceTo(buffer.End);
            if (length > 0 && result.IsCompleted)
            {
                throw new EndOfStreamException();
            }
        }
    }

    public static async ValueTask WriteIntAsync(
        this PipeWriter writer,
        int value,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var buffer = writer.GetMemory(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Span, value);
        writer.Advance(sizeof(int));

        var result = await writer.FlushAsync(cancellationToken);
        result.Check();
    }

    public static async ValueTask WriteLongAsync(
        this PipeWriter writer,
        long value,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var buffer = writer.GetMemory(sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(buffer.Span, value);
        writer.Advance(sizeof(long));

        var result = await writer.FlushAsync(cancellationToken);
        result.Check();
    }

    public static async ValueTask WriteStringAsync(
        this PipeWriter writer,
        string value,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        int totalLength = sizeof(int) + Encoding.UTF8.GetByteCount(value);
        var buffer = writer.GetMemory(totalLength);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Span, Encoding.UTF8.GetByteCount(value));
        Encoding.UTF8.GetBytes(value, buffer[sizeof(int)..].Span);
        writer.Advance(totalLength);

        var result = await writer.FlushAsync(cancellationToken);
        result.Check();
    }
}
