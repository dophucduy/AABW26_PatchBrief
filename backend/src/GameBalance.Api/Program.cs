using GameBalance.Api;
using GameBalance.Api.Community;
using GameBalance.Pipeline.Layers.L0Adaptive;
using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L0Adaptive;
using Microsoft.AspNetCore.Mvc;
using GameBalance.Pipeline.Layers.L3Metric;
using GameBalance.Pipeline.Layers.L4Context;
using GameBalance.Pipeline.Layers.L5Impact;
using GameBalance.Pipeline.Layers.L6Risk;
using GameBalance.Pipeline.Layers.L7Report;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.Configure<ApifyOptions>(
    builder.Configuration.GetSection(ApifyOptions.SectionName));
builder.Services.AddHttpClient<ApifyCommunityClient>();
builder.Services.AddSingleton<AdaptiveLayer>();
builder.Services.AddSingleton<TelemetryIngestLayer>();
builder.Services.AddSingleton<SemanticAnalyzer>();
builder.Services.AddSingleton<MetricEngine>();
builder.Services.AddSingleton<ContextBundleBuilder>();
builder.Services.AddSingleton<CommunityContextBuilder>();
builder.Services.AddSingleton<ImpactAnalyzer>();
builder.Services.AddSingleton<RiskFramer>();
builder.Services.AddSingleton<ReportGenerator>();
builder.Services.AddSingleton<AdapterStore>();
builder.Services.AddSingleton<MappingService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173",
                "http://127.0.0.1:4173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

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

app.UseCors("Frontend");
app.UseHttpsRedirection();

app.MapPost("/analyze", async (
    IFormFile telemetry_live,
    IFormFile telemetry_playtest,
    IFormFile game_definition,
    IFormFile context_bundle,
    IFormFile? adapter,
    [FromForm(Name = "community_game_name")] string? communityGameName,
    [FromForm(Name = "community_max_reviews")] int? communityMaxReviews,
    [FromForm(Name = "community_language")] string? communityLanguage,
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
    CancellationToken cancellationToken) =>
{
    try
    {
        return await AnalysisOrchestrator.RunPipelineAsync(
            telemetry_live,
            telemetry_playtest,
            game_definition,
            context_bundle,
            adapter,
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
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (HttpRequestException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status502BadGateway);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        return Results.Problem("Apify Actor run timed out", statusCode: StatusCodes.Status504GatewayTimeout);
    }
})
.WithName("AnalyzePipeline")
.Accepts<IFormFile>("multipart/form-data")
.Produces<AnalyzeResponse>()
.ProducesProblem(StatusCodes.Status400BadRequest)
.DisableAntiforgery();

app.MapPost("/community/steam/scrape", async (
    CommunityScrapeRequest request,
    ApifyCommunityClient apifyCommunityClient,
    CommunityContextBuilder communityContextBuilder,
    CancellationToken cancellationToken) =>
{
    try
    {
        ApifyCommunityResult result = await apifyCommunityClient.ScrapeAsync(
            request,
            cancellationToken);
        var gameDefinition = new GameDefinitionSnapshot
        {
            GameId = result.GameName,
            Entities = Array.Empty<GameEntity>(),
        };
        CommunitySnapshot community = new()
        {
            Clusters = communityContextBuilder.BuildClusters(
                gameDefinition,
                result.Feedback),
        };

        return Results.Ok(new CommunityScrapeResponse
        {
            GameName = result.GameName,
            RunId = result.RunId,
            DatasetId = result.DatasetId,
            ReviewCount = result.Feedback.Count,
            Community = community,
        });
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (HttpRequestException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status502BadGateway);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        return Results.Problem("Apify Actor run timed out", statusCode: StatusCodes.Status504GatewayTimeout);
    }
})
.WithName("ScrapeSteamCommunityByGameName")
.Produces<CommunityScrapeResponse>()
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status502BadGateway)
.ProducesProblem(StatusCodes.Status503ServiceUnavailable)
.ProducesProblem(StatusCodes.Status504GatewayTimeout);

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
    string fixtureRoot = AnalysisOrchestrator.ResolveFixtureRoot();
    if (!Directory.Exists(fixtureRoot))
    {
        return Results.NotFound(new { error = $"fixture directory not found: {fixtureRoot}" });
    }

    try
    {
        return await AnalysisOrchestrator.RunPipelineFromFilesAsync(
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

app.MapFrontendApi();

app.Run();

public partial class Program;

