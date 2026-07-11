using GameBalance.Pipeline.Layers.L0Adaptive;

namespace GameBalance.Tests;

/// <summary>Tests for the L0 Adaptive layer (Requirement 3).</summary>
public class L0AdaptiveTests
{
    private readonly AdaptiveLayer _layer = new();

    private static List<Dictionary<string, object?>> Events(
        params Dictionary<string, object?>[] items) => items.ToList();

    [Fact]
    public void Maps_studio_fields_to_canonical()
    {
        // AC1
        var events = Events(new Dictionary<string, object?>
        {
            ["player"] = "p1", ["hero"] = "char_A", ["ts"] = 100
        });
        var adapter = new AdapterConfig
        {
            FieldMap = new() { ["player"] = "player_id", ["hero"] = "entity_id", ["ts"] = "timestamp" }
        };

        AdapterResult result = _layer.Adapt(events, adapter);

        var e = result.Events[0];
        Assert.Equal("p1", e["player_id"]);
        Assert.Equal("char_A", e["entity_id"]);
        Assert.Equal(100, e["timestamp"]);
        Assert.False(e.ContainsKey("player"));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void No_adapter_passes_through_unchanged()
    {
        // AC2
        var events = Events(new Dictionary<string, object?>
        {
            ["player_id"] = "p1", ["entity_id"] = "char_A"
        });

        AdapterResult result = _layer.Adapt(events, null);

        Assert.Equal("p1", result.Events[0]["player_id"]);
        Assert.Equal("char_A", result.Events[0]["entity_id"]);
        // defensive copy, not the same reference
        Assert.NotSame(events[0], result.Events[0]);
    }

    [Fact]
    public void Missing_mapped_field_warns_and_skips()
    {
        // AC3
        var events = Events(new Dictionary<string, object?> { ["player"] = "p1" });
        var adapter = new AdapterConfig
        {
            FieldMap = new() { ["player"] = "player_id", ["hero"] = "entity_id" }
        };

        AdapterResult result = _layer.Adapt(events, adapter);

        Assert.Equal("p1", result.Events[0]["player_id"]);
        Assert.False(result.Events[0].ContainsKey("entity_id"));
        Assert.Contains(result.Warnings, w => w.Contains("hero"));
    }

    [Fact]
    public void Unmapped_fields_pass_through()
    {
        // AC4
        var events = Events(new Dictionary<string, object?>
        {
            ["player"] = "p1", ["score"] = 42
        });
        var adapter = new AdapterConfig { FieldMap = new() { ["player"] = "player_id" } };

        AdapterResult result = _layer.Adapt(events, adapter);

        Assert.Equal("p1", result.Events[0]["player_id"]);
        Assert.Equal(42, result.Events[0]["score"]);
    }

    [Fact]
    public void Apply_handles_both_streams()
    {
        var online = Events(new Dictionary<string, object?> { ["hero"] = "char_A" });
        var offline = Events(new Dictionary<string, object?> { ["hero"] = "char_B" });
        var adapter = new AdapterConfig { FieldMap = new() { ["hero"] = "entity_id" } };

        var (on, off) = _layer.Apply(online, offline, adapter);

        Assert.Equal("char_A", on.Events[0]["entity_id"]);
        Assert.Equal("char_B", off.Events[0]["entity_id"]);
    }

    [Fact]
    public void Parse_supports_metric_map_and_legacy_field_map()
    {
        AdapterConfig metric = AdapterConfig.Parse("""{"metric_map":{"hero":"entity_id"}}""");
        AdapterConfig field = AdapterConfig.Parse("""{"field_map":{"hero":"entity_id"}}""");
        AdapterConfig bare = AdapterConfig.Parse("""{"hero":"entity_id"}""");

        Assert.Equal("entity_id", metric.MetricMap["hero"]);
        Assert.Equal("entity_id", field.FieldMap["hero"]);
        Assert.Equal("entity_id", bare.FieldMap["hero"]);
    }
}
