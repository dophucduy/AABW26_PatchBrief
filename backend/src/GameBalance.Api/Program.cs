using GameBalance.Api;
using GameBalance.Pipeline.Layers.L0Adaptive;
using GameBalance.Pipeline.Layers.L1Ingest;
using GameBalance.Pipeline.Layers.L2Semantic;
using GameBalance.Pipeline.Layers.L3Metric;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<AdaptiveLayer>();
builder.Services.AddSingleton<IngestNormalizeLayer>();
builder.Services.AddSingleton<SemanticAnalyzer>();
builder.Services.AddSingleton<MetricEngine>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
    IFormFile player_data,
    [FromForm] string source,
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
        EventSource selectedSource = EventSourceExtensions.ParseTag(source);
        IReadOnlyList<Dictionary<string, object?>> rawEvents =
            await JsonUploadParser.ParseEventsAsync(player_data, "player_data", cancellationToken);
        string gameDefinitionJson = await JsonUploadParser.ReadTextAsync(game_definition, cancellationToken);
        string? adapterJson = adapter is null
            ? null
            : await JsonUploadParser.ReadTextAsync(adapter, cancellationToken);

        AdapterResult adapted = adaptiveLayer.Adapt(rawEvents, AdapterConfig.Parse(adapterJson));
        IngestResult ingest = ingestLayer.Normalize(adapted.Events, selectedSource);
        SemanticResult semantic = semanticAnalyzer.Analyze(ingest.Events, gameDefinitionJson);
        MetricResult metric = metricEngine.Compute(ingest.Events);

        string sourceTag = selectedSource.ToTag();
        IReadOnlyList<string> adapterWarnings = adapted.Warnings
            .Select(warning => $"[{sourceTag}] {warning}")
            .ToList();

        var response = new AnalyzeResponse
        {
            SelectedSource = sourceTag,
            Adapter = new AdapterStageResponse
            {
                Source = sourceTag,
                EventCount = adapted.Events.Count,
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
.WithName("AnalyzeSelectedTelemetrySourceL0ToL3")
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
