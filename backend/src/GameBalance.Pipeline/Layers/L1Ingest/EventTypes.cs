namespace GameBalance.Pipeline.Layers.L1Ingest;

/// <summary>
/// The canonical set of supported telemetry event types (Requirement 4, MVP cut).
/// Any event whose <c>event_type</c> is not in this set is discarded by L1.
/// </summary>
public static class EventTypes
{
    public const string SessionStart = "session_start";
    public const string MatchEnd = "match_end";
    public const string Death = "death";
    public const string AbilityUsed = "ability_used";
    public const string EntityPick = "entity_pick";
    public const string AreaEnter = "area_enter";

    /// <summary>All recognized event types.</summary>
    public static readonly IReadOnlySet<string> Supported = new HashSet<string>
    {
        SessionStart,
        MatchEnd,
        Death,
        AbilityUsed,
        EntityPick,
        AreaEnter,
    };

    public static bool IsSupported(string? eventType) =>
        eventType is not null && Supported.Contains(eventType);
}
