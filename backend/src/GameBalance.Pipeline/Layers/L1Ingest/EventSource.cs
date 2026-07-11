namespace GameBalance.Pipeline.Layers.L1Ingest;

/// <summary>
/// Origin of a telemetry event stream. L1 tags every normalized event with its
/// source so downstream layers can compare playtest (offline) vs live (online).
/// </summary>
public enum EventSource
{
    Online,
    Offline,
}

public static class EventSourceExtensions
{
    /// <summary>The canonical string written into the event's <c>source</c> field.</summary>
    public static string ToTag(this EventSource source) => source switch
    {
        EventSource.Online => "online",
        EventSource.Offline => "offline",
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };
}
