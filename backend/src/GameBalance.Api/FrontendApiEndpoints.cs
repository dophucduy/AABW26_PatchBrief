using System.Text.Json;
using GameBalance.Api.Community;
using GameBalance.Pipeline.Layers.L0Adaptive;
using GameBalance.Pipeline.Layers.L1Ingest;
using GameBalance.Pipeline.Layers.L2Semantic;
using GameBalance.Pipeline.Layers.L3Metric;
using GameBalance.Pipeline.Layers.L4Context;
using GameBalance.Pipeline.Layers.L5Impact;
using GameBalance.Pipeline.Layers.L6Risk;
using GameBalance.Pipeline.Layers.L7Report;
using Microsoft.AspNetCore.Mvc;

namespace GameBalance.Api;

internal static class FrontendApiEndpoints
{
    public static void MapFrontendApi(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/analyze/health", () => Results.Ok(new
        {
            status = "ok",
            service = "GameBalance.Api",
            timestamp = DateTime.UtcNow,
        }))
        .WithName("FrontendHealth");

        api.MapGet("/mapping", (AdapterStore adapterStore) =>
            Results.Ok(new { adapters = adapterStore.List() }))
        .WithName("FrontendListAdapters");

        api.MapPost("/mapping/suggest", async (
            IFormFile sampleFile,
            [FromForm] string? genre,
            MappingService mappingService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                string sampleJson = await JsonUploadParser.ReadTextAsync(sampleFile, cancellationToken);
                return Results.Ok(mappingService.Suggest(sampleJson, genre));
            }
            catch (Exception exception) when (exception is InvalidDataException or JsonException)
            {
                return ApiErrors.BadRequest(exception.Message);
            }
        })
        .DisableAntiforgery()
        .WithName("FrontendSuggestMapping");

        api.MapPost("/mapping/preview", async (
            IFormFile sampleFile,
            [FromForm] string fieldMap,
            [FromForm] string eventMap,
            MappingService mappingService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                string sampleJson = await JsonUploadParser.ReadTextAsync(sampleFile, cancellationToken);
                List<MappingRowRequest> fieldRows = JsonSerializer.Deserialize<List<MappingRowRequest>>(fieldMap) ?? [];
                List<MappingRowRequest> eventRows = JsonSerializer.Deserialize<List<MappingRowRequest>>(eventMap) ?? [];
                return Results.Ok(mappingService.Preview(sampleJson, fieldRows, eventRows));
            }
            catch (Exception exception) when (exception is InvalidDataException or JsonException)
            {
                return ApiErrors.BadRequest(exception.Message);
            }
        })
        .DisableAntiforgery()
        .WithName("FrontendPreviewMapping");

        api.MapPost("/mapping/confirm", ([FromBody] AdapterConfirmRequest request, AdapterStore adapterStore) =>
        {
            if (string.IsNullOrWhiteSpace(request.AdapterId))
            {
                return ApiErrors.BadRequest("adapter_id is required");
            }

            adapterStore.Save(request);
            return Results.Ok(new
            {
                adapter_id = request.AdapterId,
                saved = true,
            });
        })
        .WithName("FrontendConfirmMapping");

        api.MapPost("/analyze", async (
            IFormFile telemetry,
            IFormFile gameDefinition,
            IFormFile rules,
            IFormFile updatePlan,
            [FromForm] string gameName,
            [FromForm] string? adapterId,
            AdapterStore adapterStore,
            AdaptiveLayer adaptiveLayer,
            TelemetryIngestLayer telemetryIngestLayer,
            SemanticAnalyzer semanticAnalyzer,
            MetricEngine metricEngine,
            ContextBundleBuilder contextBundleBuilder,
            CommunityContextBuilder communityContextBuilder,
            ApifyCommunityClient apifyCommunityClient,
            ImpactAnalyzer impactAnalyzer,
            RiskFramer riskFramer,
            ReportGenerator reportGenerator,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gameName))
                {
                    return ApiErrors.BadRequest("gameName is required");
                }

                string telemetryJson = await JsonUploadParser.ReadTextAsync(telemetry, cancellationToken);
                string gameDefinitionJson = await JsonUploadParser.ReadTextAsync(gameDefinition, cancellationToken);
                string rulesJson = await JsonUploadParser.ReadTextAsync(rules, cancellationToken);
                string updatePlanJson = await JsonUploadParser.ReadTextAsync(updatePlan, cancellationToken);

                string? adapterJson = null;
                if (!string.IsNullOrWhiteSpace(adapterId))
                {
                    if (!adapterStore.TryGet(adapterId, out adapterJson))
                    {
                        return ApiErrors.BadRequest($"adapter '{adapterId}' was not found");
                    }
                }

                return await AnalysisOrchestrator.RunFrontendPipelineAsync(
                    telemetryJson,
                    gameDefinitionJson,
                    rulesJson,
                    updatePlanJson,
                    gameName,
                    adapterJson,
                    adaptiveLayer,
                    telemetryIngestLayer,
                    semanticAnalyzer,
                    metricEngine,
                    contextBundleBuilder,
                    communityContextBuilder,
                    apifyCommunityClient,
                    impactAnalyzer,
                    riskFramer,
                    reportGenerator,
                    httpClientFactory,
                    configuration,
                    cancellationToken);
            }
            catch (InvalidDataException exception)
            {
                return ApiErrors.BadRequest(exception.Message);
            }
            catch (JsonException exception)
            {
                return ApiErrors.BadRequest($"invalid JSON input: {exception.Message}");
            }
            catch (ArgumentException exception)
            {
                return ApiErrors.BadRequest(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return Results.Problem(
                    detail: exception.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    extensions: new Dictionary<string, object?> { ["error"] = new { message = exception.Message } });
            }
            catch (HttpRequestException exception)
            {
                return Results.Problem(
                    detail: exception.Message,
                    statusCode: StatusCodes.Status502BadGateway,
                    extensions: new Dictionary<string, object?> { ["error"] = new { message = exception.Message } });
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Results.Problem(
                    detail: "Steam scrape timed out",
                    statusCode: StatusCodes.Status504GatewayTimeout,
                    extensions: new Dictionary<string, object?> { ["error"] = new { message = "Steam scrape timed out" } });
            }
        })
        .DisableAntiforgery()
        .WithName("FrontendAnalyze");

        api.MapGet("/analyze/demo/files", async (CancellationToken cancellationToken) =>
        {
            string fixtureRoot = AnalysisOrchestrator.ResolveFixtureRoot();
            if (!Directory.Exists(fixtureRoot))
            {
                return ApiErrors.NotFound($"fixture directory not found: {fixtureRoot}");
            }

            try
            {
                FrontendFixtureFiles fixtures = await FixtureLoader.LoadDemoCaseAsync(fixtureRoot, cancellationToken);
                return Results.Ok(new
                {
                    telemetry = fixtures.TelemetryJson,
                    game_definition = fixtures.GameDefinitionJson,
                    rules = fixtures.RulesJson,
                    update_plan = fixtures.UpdatePlanJson,
                    adapter_id = "demo_moba",
                });
            }
            catch (Exception exception) when (exception is InvalidDataException or JsonException or ArgumentException)
            {
                return ApiErrors.BadRequest(exception.Message);
            }
        })
        .WithName("FrontendAnalyzeDemoFiles");

        api.MapGet("/analyze/demo", async (
            AdaptiveLayer adaptiveLayer,
            TelemetryIngestLayer telemetryIngestLayer,
            SemanticAnalyzer semanticAnalyzer,
            MetricEngine metricEngine,
            ContextBundleBuilder contextBundleBuilder,
            ImpactAnalyzer impactAnalyzer,
            RiskFramer riskFramer,
            ReportGenerator reportGenerator,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            string fixtureRoot = AnalysisOrchestrator.ResolveFixtureRoot();
            if (!Directory.Exists(fixtureRoot))
            {
                return ApiErrors.NotFound($"fixture directory not found: {fixtureRoot}");
            }

            try
            {
                FrontendFixtureFiles fixtures = await FixtureLoader.LoadDemoCaseAsync(fixtureRoot, cancellationToken);
                return await AnalysisOrchestrator.RunFrontendPipelineAsync(
                    fixtures.TelemetryJson,
                    fixtures.GameDefinitionJson,
                    fixtures.RulesJson,
                    fixtures.UpdatePlanJson,
                    gameName: null,
                    fixtures.AdapterJson,
                    adaptiveLayer,
                    telemetryIngestLayer,
                    semanticAnalyzer,
                    metricEngine,
                    contextBundleBuilder,
                    communityContextBuilder: null,
                    apifyCommunityClient: null,
                    impactAnalyzer,
                    riskFramer,
                    reportGenerator,
                    httpClientFactory,
                    configuration,
                    cancellationToken,
                    seedCommunityJson: fixtures.CommunityJson);
            }
            catch (Exception exception) when (exception is InvalidDataException or JsonException or ArgumentException)
            {
                return ApiErrors.BadRequest(exception.Message);
            }
        })
        .WithName("FrontendAnalyzeDemo");
    }
}

internal static class ApiErrors
{
    public static IResult BadRequest(string message) =>
        Results.BadRequest(new { error = new { message } });

    public static IResult NotFound(string message) =>
        Results.NotFound(new { error = new { message } });
}
