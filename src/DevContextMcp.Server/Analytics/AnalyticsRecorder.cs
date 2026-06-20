using System.Threading.Channels;
using DevContextMcp.Server.Core.Models.Analytics;

namespace DevContextMcp.Server.Analytics;

/// <summary>
/// Buffers captured events on a bounded channel drained by <see cref="AnalyticsWriterHostedService"/>.
/// Enqueue is non-blocking; when the buffer is full the oldest event is dropped and counted.
/// </summary>
internal sealed class AnalyticsRecorder : IAnalyticsRecorder
{
    private const int Capacity = 10_000;

    private readonly Channel<ToolInvocationRecord> _channel;
    private long _droppedCount;

    public AnalyticsRecorder()
    {
        _channel = Channel.CreateBounded<ToolInvocationRecord>(
            new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            },
            itemDropped: _ => Interlocked.Increment(ref _droppedCount));
    }

    public bool Enabled => true;

    /// <summary>
    /// Reader drained by the background writer.
    /// </summary>
    public ChannelReader<ToolInvocationRecord> Reader => _channel.Reader;

    /// <summary>
    /// Number of events dropped under back-pressure since startup.
    /// </summary>
    public long DroppedCount => Interlocked.Read(ref _droppedCount);

    public void Record(ToolInvocationRecord record) => _channel.Writer.TryWrite(record);
}
