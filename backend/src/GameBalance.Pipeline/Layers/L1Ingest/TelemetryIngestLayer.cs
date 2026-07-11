using System.Globalization;

namespace GameBalance.Pipeline.Layers.L1Ingest;

/// <summary>
/// Describes a telemetry row discarded during ingest.
/// </summary>
public sealed record DiscardedTelemetryRow(int Index, string Source, string EntityId, string Reason);

/// <summary>
/// Result of L1 telemetry ingest: validated metric rows tagged with live/playtest source.
/// </summary>
public sealed class TelemetryIngestResult
{
    public required IReadOnlyList<Dictionary<string, object?>> Records { get; init; }

    public IReadOnlyList<DiscardedTelemetryRow> Discarded { get; init; } =
        Array.Empty<DiscardedTelemetryRow>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public int AcceptedCount => Records.Count;

    public int DiscardedCount => Discarded.Count;

    public IReadOnlyList<string> Sources => Records
        .Select(item => item.TryGetValue("source", out object? value) ? value?.ToString() : null)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToList();
}

/// <summary>
/// Validates structured telemetry rows (win/pick/economy aggregates) for L3+.
/// </summary>
public sealed class TelemetryIngestLayer
{
    private const string SourceField = "source";
    private const string EntityIdField = "entity_id";
    private const string BracketIdField = "bracket_id";

    private static readonly string[] MetricFields =
    [
        "sessions",
        "win_rate",
        "pick_rate",
        "death_rate",
        "economy",
    ];

    public TelemetryIngestResult Normalize(
        IReadOnlyList<Dictionary<string, object?>> liveRecords,
        IReadOnlyList<Dictionary<string, object?>> playtestRecords)
    {
        var accepted = new List<Dictionary<string, object?>>(liveRecords.Count + playtestRecords.Count);
        var discarded = new List<DiscardedTelemetryRow>();
        var warnings = new List<string>();

        Ingest(liveRecords, EventSource.Online, accepted, discarded, warnings);
        Ingest(playtestRecords, EventSource.Offline, accepted, discarded, warnings);

        return new TelemetryIngestResult
        {
            Records = accepted,
            Discarded = discarded,
            Warnings = warnings,
        };
    }

    private static void Ingest(
        IReadOnlyList<Dictionary<string, object?>> records,
        EventSource source,
        List<Dictionary<string, object?>> accepted,
        List<DiscardedTelemetryRow> discarded,
        List<string> warnings)
    {
        string sourceTag = source.ToTag();

        for (int index = 0; index < records.Count; index++)
        {
            Dictionary<string, object?> row = records[index];
            string entityId = ReadString(row, EntityIdField) ?? "<missing>";
            string bracketId = ReadString(row, BracketIdField) ?? "<missing>";

            var missing = new List<string>();
            if (!HasValue(row, EntityIdField))
            {
                missing.Add(EntityIdField);
            }

            if (!HasValue(row, BracketIdField))
            {
                missing.Add(BracketIdField);
            }

            if (missing.Count > 0)
            {
                string reason = $"missing required field(s): {string.Join(", ", missing)}";
                discarded.Add(new DiscardedTelemetryRow(index, sourceTag, entityId, reason));
                warnings.Add($"[{sourceTag}] row {index} ({entityId}): {reason}; discarded");
                continue;
            }

            if (!HasAnyMetric(row))
            {
                string reason = "missing metric signal (sessions, win_rate, pick_rate, death_rate, or economy)";
                discarded.Add(new DiscardedTelemetryRow(index, sourceTag, entityId, reason));
                warnings.Add($"[{sourceTag}] row {index} ({entityId}): {reason}; discarded");
                continue;
            }

            if (!ValidateRates(row, out string? rateError))
            {
                discarded.Add(new DiscardedTelemetryRow(index, sourceTag, entityId, rateError!));
                warnings.Add($"[{sourceTag}] row {index} ({entityId}): {rateError}; discarded");
                continue;
            }

            var normalized = new Dictionary<string, object?>(row)
            {
                [SourceField] = ReadString(row, SourceField) ?? sourceTag,
                [EntityIdField] = entityId,
                [BracketIdField] = bracketId,
            };
            accepted.Add(normalized);
        }
    }

    private static bool HasAnyMetric(Dictionary<string, object?> row) =>
        MetricFields.Any(field => HasValue(row, field));

    private static bool ValidateRates(Dictionary<string, object?> row, out string? error)
    {
        foreach (string field in new[] { "win_rate", "pick_rate", "death_rate" })
        {
            if (!HasValue(row, field))
            {
                continue;
            }

            if (!TryReadDouble(row[field], out double value))
            {
                error = $"{field} must be numeric";
                return false;
            }

            if (value is < 0 or > 1)
            {
                error = $"{field} must be between 0 and 1";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool HasValue(Dictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out object? value) && value is not null &&
        !(value is string s && string.IsNullOrWhiteSpace(s));

    private static string? ReadString(Dictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out object? value) ? value?.ToString() : null;

    private static bool TryReadDouble(object? value, out double number)
    {
        switch (value)
        {
            case double d:
                number = d;
                return true;
            case float f:
                number = f;
                return true;
            case int i:
                number = i;
                return true;
            case long l:
                number = l;
                return true;
            case decimal m:
                number = (double)m;
                return true;
            case string s:
                return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
            default:
                number = 0;
                return false;
        }
    }
}
