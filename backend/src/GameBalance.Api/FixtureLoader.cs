using System.Text.Json;
using System.Text.Json.Serialization;
using GameBalance.Pipeline.Contracts;

namespace GameBalance.Api;

internal static class FixtureLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task<FrontendFixtureFiles> LoadDemoCaseAsync(
        string fixtureRoot,
        CancellationToken cancellationToken = default)
    {
        string telemetryJson = await ReadOrExtractTelemetryAsync(fixtureRoot, cancellationToken);
        string adapterJson = await File.ReadAllTextAsync(
            Path.Combine(fixtureRoot, "adapter.json"),
            cancellationToken);

        string gameDefinitionJson = await ReadOrExtractAsync(
            fixtureRoot,
            "game_definition.json",
            bundle => JsonSerializer.Serialize(bundle.GameDefinition, JsonOptions),
            cancellationToken);
        string rulesJson = await ReadOrExtractAsync(
            fixtureRoot,
            "rules.json",
            bundle => JsonSerializer.Serialize(bundle.Rules, JsonOptions),
            cancellationToken);
        string updatePlanJson = await ReadOrExtractAsync(
            fixtureRoot,
            "update_plan.json",
            bundle => JsonSerializer.Serialize(bundle.UpdatePlan, JsonOptions),
            cancellationToken);
        string communityJson = await ReadOrExtractAsync(
            fixtureRoot,
            "community.json",
            bundle => JsonSerializer.Serialize(bundle.Community, JsonOptions),
            cancellationToken);

        return new FrontendFixtureFiles
        {
            TelemetryJson = telemetryJson,
            GameDefinitionJson = gameDefinitionJson,
            RulesJson = rulesJson,
            UpdatePlanJson = updatePlanJson,
            CommunityJson = communityJson,
            AdapterJson = adapterJson,
        };
    }

    private static async Task<string> ReadOrExtractTelemetryAsync(
        string fixtureRoot,
        CancellationToken cancellationToken)
    {
        string telemetryPath = Path.Combine(fixtureRoot, "telemetry.json");
        if (File.Exists(telemetryPath))
        {
            return await File.ReadAllTextAsync(telemetryPath, cancellationToken);
        }

        return await File.ReadAllTextAsync(
            Path.Combine(fixtureRoot, "telemetry_live.json"),
            cancellationToken);
    }

    private static async Task<string> ReadOrExtractAsync(
        string fixtureRoot,
        string fileName,
        Func<ContextBundle, string> extract,
        CancellationToken cancellationToken)
    {
        string path = Path.Combine(fixtureRoot, fileName);
        if (File.Exists(path))
        {
            return await File.ReadAllTextAsync(path, cancellationToken);
        }

        string bundlePath = Path.Combine(fixtureRoot, "context_bundle.json");
        ContextBundle bundle = ContextBundleParser.Parse(await File.ReadAllTextAsync(bundlePath, cancellationToken));
        return extract(bundle);
    }
}

internal sealed class FrontendFixtureFiles
{
    public required string TelemetryJson { get; init; }
    public required string GameDefinitionJson { get; init; }
    public required string RulesJson { get; init; }
    public required string UpdatePlanJson { get; init; }
    public required string CommunityJson { get; init; }
    public required string AdapterJson { get; init; }
}
