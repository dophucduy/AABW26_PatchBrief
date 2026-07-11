using GameBalance.Api;
using GameBalance.Pipeline.Layers.L0Adaptive;
using GameBalance.Pipeline.Layers.L1Ingest;
using GameBalance.Pipeline.Layers.L2Semantic;
using GameBalance.Pipeline.Layers.L3Metric;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<AdaptiveLayer>();
builder.Services.AddSingleton<IngestNormalizeLayer>();
builder.Services.AddSingleton<SemanticAnalyzer>();
builder.Services.AddSingleton<MetricEngine>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/analyze", async (
    IFormFile player_online,
    IFormFile player_offline,
    IFormFile game_definition,
    IFormFile? adapter,
    AdaptiveLayer adaptiveLayer,
    IngestNormalizeLayer ingestLayer,
    SemanticAnalyzer semanticAnalyzer,
    MetricEngine metricEngine,
    CancellationToken cancellationToken) =>
{
    try
    {
        IReadOnlyList<Dictionary<string, object?>> onlineEvents =
            await JsonUploadParser.ParseEventsAsync(player_online, "player_online", cancellationToken);
        IReadOnlyList<Dictionary<string, object?>> offlineEvents =
            await JsonUploadParser.ParseEventsAsync(player_offline, "player_offline", cancellationToken);
        string gameDefinitionJson = await JsonUploadParser.ReadTextAsync(game_definition, cancellationToken);
        string? adapterJson = adapter is null
            ? null
            : await JsonUploadParser.ReadTextAsync(adapter, cancellationToken);

        var (adaptedOnline, adaptedOffline) = adaptiveLayer.Apply(
            onlineEvents,
            offlineEvents,
            AdapterConfig.Parse(adapterJson));
        IngestResult ingest = ingestLayer.Normalize(adaptedOnline.Events, adaptedOffline.Events);
        SemanticResult semantic = semanticAnalyzer.Analyze(ingest.Events, gameDefinitionJson);
        MetricResult metric = metricEngine.Compute(ingest.Events);

        IReadOnlyList<string> adapterWarnings = adaptedOnline.Warnings
            .Select(warning => $"[online] {warning}")
            .Concat(adaptedOffline.Warnings.Select(warning => $"[offline] {warning}"))
            .ToList();

        var response = new AnalyzeResponse
        {
            Adapter = new AdapterStageResponse
            {
                OnlineEventCount = adaptedOnline.Events.Count,
                OfflineEventCount = adaptedOffline.Events.Count,
                Warnings = adapterWarnings,
            },
            Ingest = ingest,
            Semantic = semantic,
            Metric = metric,
            Warnings = adapterWarnings
                .Concat(ingest.Warnings)
                .Concat(semantic.Warnings)
                .ToList(),
        };
        return Results.Ok(response);
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
.WithName("AnalyzeTelemetryL0ToL3")
.Accepts<IFormFile>("multipart/form-data")
.Produces<AnalyzeResponse>()
.ProducesProblem(StatusCodes.Status400BadRequest)
.DisableAntiforgery();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
