using GameBalance.Api;
using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L0Adaptive;
using GameBalance.Pipeline.Layers.L1Ingest;
using GameBalance.Pipeline.Layers.L2Semantic;
using GameBalance.Pipeline.Layers.L3Metric;
using GameBalance.Pipeline.Layers.L5Impact;
using GameBalance.Pipeline.Layers.L6Risk;
using GameBalance.Pipeline.Layers.L7Report;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AdaptiveLayer>();
builder.Services.AddSingleton<TelemetryIngestLayer>();
builder.Services.AddSingleton<SemanticAnalyzer>();
builder.Services.AddSingleton<MetricEngine>();
builder.Services.AddSingleton<ImpactAnalyzer>();
builder.Services.AddSingleton<RiskFramer>();
builder.Services.AddSingleton<ReportGenerator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "GameBalance API v1");
    });
}

app.UseHttpsRedirection();

app.MapPost("/analyze", async (
    IFormFile telemetry_live,
    IFormFile telemetry_playtest,
    IFormFile game_definition,
    IFormFile context_bundle,
    IFormFile? adapter,
    AdaptiveLayer adaptiveLayer,
    TelemetryIngestLayer telemetryIngestLayer,
    SemanticAnalyzer semanticAnalyzer,
    MetricEngine metricEngine,
    ImpactAnalyzer impactAnalyzer,
    RiskFramer riskFramer,
    ReportGenerator reportGenerator,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    try
    {
        return await RunPipelineAsync(
            telemetry_live,
            telemetry_playtest,
            game_definition,
            context_bundle,
            adapter,
            adaptiveLayer,
            telemetryIngestLayer,
            semanticAnalyzer,
            metricEngine,
            impactAnalyzer,
            riskFramer,
            reportGenerator,
            httpClientFactory,
            configuration,
            cancellationToken);
    }
    catch (InvalidDataException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (System.Text.Json.JsonException exception)
    {
        return Results.BadRequest(new { error = $"invalid JSON input: {exception.Message}" });
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
})
.WithName("AnalyzePipeline")
.Accepts<IFormFile>("multipart/form-data")
.Produces<AnalyzeResponse>()
.ProducesProblem(StatusCodes.Status400BadRequest)
.DisableAntiforgery();

app.MapGet("/analyze/demo", async (
    AdaptiveLayer adaptiveLayer,
    TelemetryIngestLayer telemetryIngestLayer,
    SemanticAnalyzer semanticAnalyzer,
    MetricEngine metricEngine,
    ImpactAnalyzer impactAnalyzer,
    RiskFramer riskFramer,
    ReportGenerator reportGenerator,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    string fixtureRoot = ResolveFixtureRoot();
    if (!Directory.Exists(fixtureRoot))
    {
        return Results.NotFound(new { error = $"fixture directory not found: {fixtureRoot}" });
    }

    try
    {
        return await RunPipelineFromFilesAsync(
            Path.Combine(fixtureRoot, "telemetry_live.json"),
            Path.Combine(fixtureRoot, "telemetry_playtest.json"),
            Path.Combine(fixtureRoot, "game_definition.json"),
            Path.Combine(fixtureRoot, "context_bundle.json"),
            Path.Combine(fixtureRoot, "adapter.json"),
            adaptiveLayer,
            telemetryIngestLayer,
            semanticAnalyzer,
            metricEngine,
            impactAnalyzer,
            riskFramer,
            reportGenerator,
            httpClientFactory,
            configuration,
            cancellationToken);
    }
    catch (Exception exception) when (exception is InvalidDataException or System.Text.Json.JsonException or ArgumentException)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
})
.WithName("AnalyzeDemoCase")
.Produces<AnalyzeResponse>()
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status400BadRequest);

app.Run();

static async Task<IResult> RunPipelineAsync(
    IFormFile telemetryLive,
    IFormFile telemetryPlaytest,
    IFormFile gameDefinition,
    IFormFile contextBundle,
    IFormFile? adapter,
    AdaptiveLayer adaptiveLayer,
    TelemetryIngestLayer telemetryIngestLayer,
    SemanticAnalyzer semanticAnalyzer,
    MetricEngine metricEngine,
    ImpactAnalyzer impactAnalyzer,
    RiskFramer riskFramer,
    ReportGenerator reportGenerator,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken)
{
    string liveJson = await JsonUploadParser.ReadTextAsync(telemetryLive, cancellationToken);
    string playtestJson = await JsonUploadParser.ReadTextAsync(telemetryPlaytest, cancellationToken);
    string gameDefinitionJson = await JsonUploadParser.ReadTextAsync(gameDefinition, cancellationToken);
    string contextBundleJson = await JsonUploadParser.ReadTextAsync(contextBundle, cancellationToken);
    string? adapterJson = adapter is null
        ? null
        : await JsonUploadParser.ReadTextAsync(adapter, cancellationToken);

    return await ExecutePipelineAsync(
        liveJson,
        playtestJson,
        gameDefinitionJson,
        contextBundleJson,
        adapterJson,
        adaptiveLayer,
        telemetryIngestLayer,
        semanticAnalyzer,
        metricEngine,
        impactAnalyzer,
        riskFramer,
        reportGenerator,
        httpClientFactory,
        configuration,
        cancellationToken);
}

static async Task<IResult> RunPipelineFromFilesAsync(
    string telemetryLivePath,
    string telemetryPlaytestPath,
    string gameDefinitionPath,
    string contextBundlePath,
    string adapterPath,
    AdaptiveLayer adaptiveLayer,
    TelemetryIngestLayer telemetryIngestLayer,
    SemanticAnalyzer semanticAnalyzer,
    MetricEngine metricEngine,
    ImpactAnalyzer impactAnalyzer,
    RiskFramer riskFramer,
    ReportGenerator reportGenerator,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken)
{
    string liveJson = await File.ReadAllTextAsync(telemetryLivePath, cancellationToken);
    string playtestJson = await File.ReadAllTextAsync(telemetryPlaytestPath, cancellationToken);
    string gameDefinitionJson = await File.ReadAllTextAsync(gameDefinitionPath, cancellationToken);
    string contextBundleJson = await File.ReadAllTextAsync(contextBundlePath, cancellationToken);
    string? adapterJson = File.Exists(adapterPath)
        ? await File.ReadAllTextAsync(adapterPath, cancellationToken)
        : null;

    return await ExecutePipelineAsync(
        liveJson,
        playtestJson,
        gameDefinitionJson,
        contextBundleJson,
        adapterJson,
        adaptiveLayer,
        telemetryIngestLayer,
        semanticAnalyzer,
        metricEngine,
        impactAnalyzer,
        riskFramer,
        reportGenerator,
        httpClientFactory,
        configuration,
        cancellationToken);
}

static async Task<IResult> ExecutePipelineAsync(
    string liveTelemetryJson,
    string playtestTelemetryJson,
    string gameDefinitionJson,
    string contextBundleJson,
    string? adapterJson,
    AdaptiveLayer adaptiveLayer,
    TelemetryIngestLayer telemetryIngestLayer,
    SemanticAnalyzer semanticAnalyzer,
    MetricEngine metricEngine,
    ImpactAnalyzer impactAnalyzer,
    RiskFramer riskFramer,
    ReportGenerator reportGenerator,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken)
{
    AdapterConfig adapter = AdapterConfig.Parse(adapterJson);
    var (adaptedLive, adaptedPlaytest) = adaptiveLayer.ApplyTelemetry(
        liveTelemetryJson,
        playtestTelemetryJson,
        adapter);
    TelemetryIngestResult telemetryIngest = telemetryIngestLayer.Normalize(
        adaptedLive.Records,
        adaptedPlaytest.Records);
    MetricResult metric = metricEngine.FromTelemetry(telemetryIngest.Records);
    SemanticResult semantic = semanticAnalyzer.AnalyzeFromMetrics(metric, gameDefinitionJson);
    ContextBundle context = ContextBundleParser.Parse(contextBundleJson);
    ImpactResult impact = impactAnalyzer.Analyze(metric, context);
    RiskResult risk = riskFramer.Analyze(impact, context);
    LlmOptions llmOptions = ReadLlmOptions(configuration);
    HttpClient httpClient = httpClientFactory.CreateClient();
    httpClient.Timeout = TimeSpan.FromSeconds(llmOptions.TimeoutSeconds);
    InsightReport report = await reportGenerator.GenerateAsync(
        metric,
        context,
        impact,
        risk,
        llmOptions,
        httpClient,
        cancellationToken);

    IReadOnlyList<string> adapterWarnings = adaptedLive.Warnings
        .Select(warning => $"[live] {warning}")
        .Concat(adaptedPlaytest.Warnings.Select(warning => $"[playtest] {warning}"))
        .ToList();

    var ingest = new IngestResult
    {
        MetricRows = telemetryIngest.Records,
        Warnings = telemetryIngest.Warnings,
    };

    var response = new AnalyzeResponse
    {
        Adapter = new AdapterStageResponse
        {
            LiveRecordCount = adaptedLive.Records.Count,
            PlaytestRecordCount = adaptedPlaytest.Records.Count,
            Warnings = adapterWarnings,
        },
        Ingest = ingest,
        Semantic = semantic,
        Metric = metric,
        Context = context,
        Impact = impact,
        Risk = risk,
        Report = report,
        Warnings = adapterWarnings
            .Concat(ingest.Warnings)
            .Concat(semantic.Warnings)
            .Concat(context.Warnings)
            .ToList(),
    };
    return Results.Ok(response);
}

static LlmOptions ReadLlmOptions(IConfiguration configuration) => new()
{
    Provider = configuration["Llm:Provider"] ?? "OpenAI",
    ApiKey = configuration["Llm:ApiKey"],
    Model = configuration["Llm:Model"] ?? "gpt-4o-mini",
    BaseUrl = configuration["Llm:BaseUrl"] ?? "https://api.openai.com/v1",
    TimeoutSeconds = int.TryParse(configuration["Llm:TimeoutSeconds"], out int timeout) ? timeout : 30,
};

static string ResolveFixtureRoot()
{
    string[] candidates =
    [
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "fixtures", "demo_case")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "fixtures", "demo_case")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "fixtures", "demo_case")),
    ];

    return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
}
