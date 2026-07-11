using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameBalance.Api;

internal sealed class AdapterStore
{
    private readonly ConcurrentDictionary<string, StoredAdapter> _adapters = new(StringComparer.OrdinalIgnoreCase);

    public AdapterStore()
    {
        SeedDemoAdapter();
    }

    public IReadOnlyList<AdapterSummaryResponse> List() =>
        _adapters.Values
            .OrderBy(adapter => adapter.CreatedAt)
            .Select(adapter => new AdapterSummaryResponse
            {
                AdapterId = adapter.AdapterId,
                CreatedAt = adapter.CreatedAt,
            })
            .ToList();

    public bool TryGet(string adapterId, out string? adapterJson)
    {
        if (_adapters.TryGetValue(adapterId, out StoredAdapter? adapter))
        {
            adapterJson = adapter.Json;
            return true;
        }

        adapterJson = null;
        return false;
    }

    public void Save(AdapterConfirmRequest request)
    {
        var payload = new
        {
            field_map = request.FieldMap,
            event_map = request.EventMap,
            custom_fields = request.CustomFields,
            canonical_version = request.CanonicalVersion,
        };

        string json = JsonSerializer.Serialize(payload, JsonOptions);
        _adapters[request.AdapterId] = new StoredAdapter
        {
            AdapterId = request.AdapterId,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            Json = json,
        };
    }

    private void SeedDemoAdapter()
    {
        string fixtureRoot = AnalysisOrchestrator.ResolveFixtureRoot();
        string adapterPath = Path.Combine(fixtureRoot, "adapter.json");
        if (!File.Exists(adapterPath))
        {
            return;
        }

        _adapters["demo_moba"] = new StoredAdapter
        {
            AdapterId = "demo_moba",
            CreatedAt = "2026-07-11T08:00:00Z",
            Json = File.ReadAllText(adapterPath),
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class StoredAdapter
    {
        public required string AdapterId { get; init; }
        public required string CreatedAt { get; init; }
        public required string Json { get; init; }
    }
}

internal sealed class AdapterSummaryResponse
{
    [JsonPropertyName("adapter_id")]
    public required string AdapterId { get; init; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }
}

internal sealed class AdapterConfirmRequest
{
    [JsonPropertyName("adapter_id")]
    public required string AdapterId { get; init; }

    [JsonPropertyName("canonical_version")]
    public string CanonicalVersion { get; init; } = "1.0";

    [JsonPropertyName("field_map")]
    public Dictionary<string, string> FieldMap { get; init; } = new();

    [JsonPropertyName("event_map")]
    public Dictionary<string, string> EventMap { get; init; } = new();

    [JsonPropertyName("custom_fields")]
    public Dictionary<string, object?> CustomFields { get; init; } = new();

    [JsonPropertyName("confirmed_by_user")]
    public bool ConfirmedByUser { get; init; }
}
