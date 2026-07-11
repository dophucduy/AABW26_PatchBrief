using GameBalance.Api.Community;
using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L0Adaptive;
using GameBalance.Pipeline.Layers.L1Ingest;
using GameBalance.Pipeline.Layers.L2Semantic;
using GameBalance.Pipeline.Layers.L3Metric;
using GameBalance.Pipeline.Layers.L4Context;
using GameBalance.Pipeline.Layers.L5Impact;
using GameBalance.Pipeline.Layers.L6Risk;
using GameBalance.Pipeline.Layers.L7Report;

namespace GameBalance.Api;

internal sealed class AnalysisOrchestrator
{
    public static async Task<IResult> RunPipelineAsync(
        IFormFile telemetryLive,
        IFormFile telemetryPlaytest,
        IFormFile gameDefinition,
        IFormFile contextBundle,
        IFormFile? adapter,
        string? communityGameName,
        int? communityMaxReviews,
        string? communityLanguage,
        AdaptiveLayer adaptiveLayer,
        TelemetryIngestLayer telemetryIngestLayer,
        SemanticAnalyzer semanticAnalyzer,
        MetricEngine metricEngine,
        CommunityContextBuilder communityContextBuilder,
        ApifyCommunityClient apifyCommunityClient,
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
            communityGameName,
            communityMaxReviews,
            communityLanguage,
            adaptiveLayer,
            telemetryIngestLayer,
            semanticAnalyzer,
            metricEngine,
            communityContextBuilder,
            apifyCommunityClient,
            impactAnalyzer,
            riskFramer,
            reportGenerator,
            httpClientFactory,
            configuration,
            cancellationToken);
    }

    public static async Task<IResult> RunPipelineFromFilesAsync(
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
            communityGameName: null,
            communityMaxReviews: null,
            communityLanguage: null,
            adaptiveLayer,
            telemetryIngestLayer,
            semanticAnalyzer,
            metricEngine,
            communityContextBuilder: null,
            apifyCommunityClient: null,
            impactAnalyzer,
            riskFramer,
            reportGenerator,
            httpClientFactory,
            configuration,
            cancellationToken);
    }

    public static async Task<IResult> RunFrontendPipelineAsync(
        string telemetryJson,
        string gameDefinitionJson,
        string rulesJson,
        string updatePlanJson,
        string? gameName,
        string? adapterJson,
        AdaptiveLayer adaptiveLayer,
        TelemetryIngestLayer telemetryIngestLayer,
        SemanticAnalyzer semanticAnalyzer,
        MetricEngine metricEngine,
        ContextBundleBuilder contextBundleBuilder,
        CommunityContextBuilder? communityContextBuilder,
        ApifyCommunityClient? apifyCommunityClient,
        ImpactAnalyzer impactAnalyzer,
        RiskFramer riskFramer,
        ReportGenerator reportGenerator,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        CancellationToken cancellationToken,
        string? seedCommunityJson = null)
    {
        AdapterConfig adapter = AdapterConfig.Parse(adapterJson);
        TelemetryAdapterResult adaptedTelemetry = adaptiveLayer.AdaptTelemetry(telemetryJson, adapter);
        IReadOnlyList<Dictionary<string, object?>> emptyPlaytest = Array.Empty<Dictionary<string, object?>>();
        TelemetryIngestResult telemetryIngest = telemetryIngestLayer.Normalize(
            adaptedTelemetry.Records,
            emptyPlaytest);
        MetricResult metric = metricEngine.FromTelemetry(telemetryIngest.Records);
        SemanticResult semantic = semanticAnalyzer.AnalyzeFromMetrics(metric, gameDefinitionJson);
        string communityJson = await ResolveCommunityJsonAsync(
            seedCommunityJson,
            gameName,
            configuration,
            cancellationToken);
        ContextBundle context = contextBundleBuilder.Build(
            gameDefinitionJson,
            rulesJson,
            updatePlanJson,
            communityJson,
            metric);

        if (!string.IsNullOrWhiteSpace(gameName) && HasApifyToken(configuration))
        {
            if (communityContextBuilder is null || apifyCommunityClient is null)
            {
                throw new InvalidOperationException("Steam community scraping is unavailable");
            }

            try
            {
                var scrapeRequest = new CommunityScrapeRequest
                {
                    GameName = gameName.Trim(),
                    MaxReviews = 100,
                    Language = "english",
                };
                ApifyCommunityResult scraped = await apifyCommunityClient.ScrapeAsync(
                    scrapeRequest,
                    cancellationToken);
                context = communityContextBuilder.Merge(context, scraped.Feedback);
            }
            catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or InvalidDataException)
            {
                string fallbackCommunity = await LoadFixtureCommunityJsonAsync(cancellationToken);
                context = contextBundleBuilder.Build(
                    gameDefinitionJson,
                    rulesJson,
                    updatePlanJson,
                    fallbackCommunity,
                    metric);
                var warnings = context.Warnings.ToList();
                warnings.Add($"Steam scrape skipped: {exception.Message}");
                context = new ContextBundle
                {
                    GameDefinition = context.GameDefinition,
                    Rules = context.Rules,
                    UpdatePlan = context.UpdatePlan,
                    Community = context.Community,
                    JoinedChanges = context.JoinedChanges,
                    Warnings = warnings,
                };
            }
        }

        ImpactResult impact = impactAnalyzer.Analyze(metric, context);
        RiskResult risk = riskFramer.Analyze(impact, context);
        LlmOptions llmOptions = ReadLlmOptions(configuration);
        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(llmOptions.TimeoutSeconds, 60));
        InsightReport report = await reportGenerator.GenerateAsync(
            metric,
            context,
            impact,
            risk,
            llmOptions,
            httpClient,
            cancellationToken);

        IReadOnlyList<string> adapterWarnings = adaptedTelemetry.Warnings
            .Select(warning => $"[telemetry] {warning}")
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
                LiveRecordCount = adaptedTelemetry.Records.Count,
                PlaytestRecordCount = 0,
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

        return Results.Ok(PatchReportMapper.ToPatchReport(response));
    }

    private static async Task<IResult> ExecutePipelineAsync(
        string liveTelemetryJson,
        string playtestTelemetryJson,
        string gameDefinitionJson,
        string contextBundleJson,
        string? adapterJson,
        string? communityGameName,
        int? communityMaxReviews,
        string? communityLanguage,
        AdaptiveLayer adaptiveLayer,
        TelemetryIngestLayer telemetryIngestLayer,
        SemanticAnalyzer semanticAnalyzer,
        MetricEngine metricEngine,
        CommunityContextBuilder? communityContextBuilder,
        ApifyCommunityClient? apifyCommunityClient,
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
        CommunityScrapeResponse? communitySource = null;
        if (!string.IsNullOrWhiteSpace(communityGameName))
        {
            if (communityContextBuilder is null || apifyCommunityClient is null)
            {
                throw new InvalidOperationException("Apify community integration is unavailable");
            }

            var scrapeRequest = new CommunityScrapeRequest
            {
                GameName = communityGameName,
                MaxReviews = communityMaxReviews ?? 100,
                Language = string.IsNullOrWhiteSpace(communityLanguage)
                    ? "english"
                    : communityLanguage,
            };
            ApifyCommunityResult scraped = await apifyCommunityClient.ScrapeAsync(
                scrapeRequest,
                cancellationToken);
            context = communityContextBuilder.Merge(context, scraped.Feedback);
            communitySource = new CommunityScrapeResponse
            {
                GameName = scraped.GameName,
                RunId = scraped.RunId,
                DatasetId = scraped.DatasetId,
                ReviewCount = scraped.Feedback.Count,
                Community = context.Community,
            };
        }

        ImpactResult impact = impactAnalyzer.Analyze(metric, context);
        RiskResult risk = riskFramer.Analyze(impact, context);
        LlmOptions llmOptions = ReadLlmOptions(configuration);
        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(llmOptions.TimeoutSeconds, 60));
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
            CommunitySource = communitySource,
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

    public static LlmOptions ReadLlmOptions(IConfiguration configuration) => new()
    {
        Provider = configuration["Llm:Provider"] ?? "OpenAI",
        ApiKey = configuration["Llm:ApiKey"],
        Model = configuration["Llm:Model"] ?? "gpt-4o-mini",
        BaseUrl = configuration["Llm:BaseUrl"] ?? "https://api.openai.com/v1",
        TimeoutSeconds = int.TryParse(configuration["Llm:TimeoutSeconds"], out int timeout) ? timeout : 30,
    };

    public static string ResolveFixtureRoot()
    {
        string[] candidates =
        [
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "fixtures", "demo_case")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "fixtures", "demo_case")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "fixtures", "demo_case")),
        ];

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }

    private static async Task<string> LoadFixtureCommunityJsonAsync(CancellationToken cancellationToken)
    {
        string fixtureRoot = ResolveFixtureRoot();
        if (!Directory.Exists(fixtureRoot))
        {
            return """{"clusters":[]}""";
        }

        try
        {
            FrontendFixtureFiles fixtures = await FixtureLoader.LoadDemoCaseAsync(fixtureRoot, cancellationToken);
            return fixtures.CommunityJson;
        }
        catch
        {
            return """{"clusters":[]}""";
        }
    }

    private static bool HasApifyToken(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["Apify:ApiToken"]);

    private static async Task<string> ResolveCommunityJsonAsync(
        string? seedCommunityJson,
        string? gameName,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(seedCommunityJson))
        {
            return seedCommunityJson;
        }

        if (!string.IsNullOrWhiteSpace(gameName) && HasApifyToken(configuration))
        {
            return """{"clusters":[]}""";
        }

        return await LoadFixtureCommunityJsonAsync(cancellationToken);
    }
}
