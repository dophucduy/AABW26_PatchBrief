using GameBalance.Pipeline.Layers.L1Ingest;

namespace GameBalance.Tests;

/// <summary>Tests for the L1 Ingest &amp; Normalize layer (Requirement 4).</summary>
public class L1IngestTests
{
    private readonly IngestNormalizeLayer _layer = new();

    private static List<Dictionary<string, object?>> Events(
        params Dictionary<string, object?>[] items) => items.ToList();

    private static Dictionary<string, object?> ValidEvent(
        string type = "match_end", string entity = "char_A", object? ts = null) => new()
    {
        ["event_type"] = type,
        ["entity_id"] = entity,
        ["timestamp"] = ts ?? 1000,
    };

    [Fact]
    public void Tags_online_events_with_online_source()
    {
        // AC1
        IngestResult result = _layer.Normalize(Events(ValidEvent()), Events());

        Assert.Single(result.Events);
        Assert.Equal("online", result.Events[0]["source"]);
    }

    [Fact]
    public void Tags_offline_events_with_offline_source()
    {
        // AC2
        IngestResult result = _layer.Normalize(Events(), Events(ValidEvent()));

        Assert.Single(result.Events);
        Assert.Equal("offline", result.Events[0]["source"]);
    }

    [Theory]
    [InlineData("session_start")]
    [InlineData("match_end")]
    [InlineData("death")]
    [InlineData("ability_used")]
    [InlineData("entity_pick")]
    [InlineData("area_enter")]
    public void Accepts_all_supported_event_types(string type)
    {
        // AC3
        IngestResult result = _layer.Normalize(Events(ValidEvent(type: type)), Events());
        Assert.Single(result.Events);
    }

    [Fact]
    public void Discards_unrecognized_event_type_with_index_and_type()
    {
        // AC4
        IngestResult result = _layer.Normalize(Events(ValidEvent(type: "level_up")), Events());

        Assert.Empty(result.Events);
        DiscardedEvent d = Assert.Single(result.Discarded);
        Assert.Equal(0, d.Index);
        Assert.Equal("level_up", d.EventType);
        Assert.Contains(result.Warnings, w => w.Contains("level_up") && w.Contains("0"));
    }

    [Fact]
    public void Discards_event_missing_timestamp()
    {
        // AC5/AC6
        var ev = ValidEvent();
        ev.Remove("timestamp");

        IngestResult result = _layer.Normalize(Events(ev), Events());

        Assert.Empty(result.Events);
        Assert.Contains(result.Discarded, d => d.Reason.Contains("timestamp"));
    }

    [Fact]
    public void Discards_event_missing_entity_id()
    {
        // AC5/AC6
        var ev = ValidEvent();
        ev.Remove("entity_id");

        IngestResult result = _layer.Normalize(Events(ev), Events());

        Assert.Empty(result.Events);
        Assert.Contains(result.Discarded, d => d.Reason.Contains("entity_id"));
    }

    [Fact]
    public void Missing_both_required_fields_reported_together()
    {
        var ev = new Dictionary<string, object?> { ["event_type"] = "death" };

        IngestResult result = _layer.Normalize(Events(ev), Events());

        DiscardedEvent d = Assert.Single(result.Discarded);
        Assert.Contains("timestamp", d.Reason);
        Assert.Contains("entity_id", d.Reason);
    }

    [Fact]
    public void Does_not_mutate_input_events()
    {
        var ev = ValidEvent();
        _ = _layer.Normalize(Events(ev), Events());

        Assert.False(ev.ContainsKey("source"));
    }

    [Fact]
    public void Mixed_streams_accept_valid_and_summarize_discards()
    {
        var online = Events(ValidEvent(entity: "char_A"), ValidEvent(type: "bogus"));
        var offline = Events(ValidEvent(entity: "char_B"));

        IngestResult result = _layer.Normalize(online, offline);

        Assert.Equal(2, result.AcceptedCount);
        Assert.Equal(1, result.DiscardedCount);
        Assert.Contains(result.Events, e => (string?)e["source"] == "online");
        Assert.Contains(result.Events, e => (string?)e["source"] == "offline");
    }
}
