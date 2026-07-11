using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GameBalance.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GameBalance.Tests;

public sealed class FrontendApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private string? _fixtureRoot;

    public FrontendApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Llm:ApiKey", string.Empty);
        });
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/analyze/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("ok", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ListAdapters_IncludesDemoAdapter()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/mapping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement adapters = document.RootElement.GetProperty("adapters");
        Assert.Contains(
            adapters.EnumerateArray(),
            item => item.GetProperty("adapter_id").GetString() == "demo_moba");
    }

    [Fact]
    public async Task SuggestMapping_ReturnsFieldAndEventMaps()
    {
        using HttpClient client = CreateClient();
        using MultipartFormDataContent form = new();
        form.Add(new StringContent("MOBA"), "genre");
        form.Add(JsonFileContent("sample.json", ReadFixture("telemetry_live.json")), "sampleFile", "sample.json");

        using HttpResponseMessage response = await client.PostAsync("/api/mapping/suggest", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(document.RootElement.GetProperty("field_map").GetArrayLength() > 0);
    }

    [Fact]
    public async Task PreviewMapping_ReturnsCounts()
    {
        using HttpClient client = CreateClient();
        using MultipartFormDataContent form = new();
        form.Add(JsonFileContent("sample.json", ReadFixture("telemetry_live.json")), "sampleFile", "sample.json");
        form.Add(new StringContent("""[{"source":"hero","target":"entity_id","confidence":0.9,"kind":"Field"}]"""), "fieldMap");
        form.Add(new StringContent("[]"), "eventMap");

        using HttpResponseMessage response = await client.PostAsync("/api/mapping/preview", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(document.RootElement.GetProperty("events_parsed").GetInt32() >= 0);
        Assert.True(document.RootElement.TryGetProperty("events_skipped", out _));
    }

    [Fact]
    public async Task ConfirmMapping_PersistsAdapter()
    {
        using HttpClient client = CreateClient();
        var payload = new
        {
            adapter_id = "integration_test_adapter",
            canonical_version = "1.0",
            field_map = new Dictionary<string, string> { ["hero"] = "entity_id" },
            event_map = new Dictionary<string, string>(),
            custom_fields = new Dictionary<string, object>(),
            confirmed_by_user = true,
        };

        using HttpResponseMessage saveResponse = await client.PostAsJsonAsync("/api/mapping/confirm", payload);
        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);

        using HttpResponseMessage listResponse = await client.GetAsync("/api/mapping");
        using JsonDocument document = await JsonDocument.ParseAsync(await listResponse.Content.ReadAsStreamAsync());
        Assert.Contains(
            document.RootElement.GetProperty("adapters").EnumerateArray(),
            item => item.GetProperty("adapter_id").GetString() == "integration_test_adapter");
    }

    [Fact]
    public async Task AnalyzeDemo_ReturnsPatchReport()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/analyze/demo");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("report_id").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("executive_summary").GetString()));
        Assert.True(document.RootElement.GetProperty("risks").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Analyze_RequiresGameName()
    {
        FrontendFixtureFiles fixtures = await FixtureLoader.LoadDemoCaseAsync(FixtureRoot);
        using HttpClient client = CreateClient();
        using MultipartFormDataContent form = new();
        form.Add(JsonFileContent("telemetry.json", fixtures.TelemetryJson), "telemetry", "telemetry.json");
        form.Add(JsonFileContent("game_definition.json", fixtures.GameDefinitionJson), "gameDefinition", "game_definition.json");
        form.Add(JsonFileContent("rules.json", fixtures.RulesJson), "rules", "rules.json");
        form.Add(JsonFileContent("update_plan.json", fixtures.UpdatePlanJson), "updatePlan", "update_plan.json");
        form.Add(new StringContent("demo_moba"), "adapterId");

        using HttpResponseMessage response = await client.PostAsync("/api/analyze", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:5173");
        return client;
    }

    private string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(FixtureRoot, fileName));

    private string FixtureRoot => _fixtureRoot ??= ResolveFixtureRoot();

    private static ByteArrayContent JsonFileContent(string fileName, string content)
    {
        ByteArrayContent fileContent = new(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return fileContent;
    }

    private static string ResolveFixtureRoot()
    {
        string[] candidates =
        [
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "fixtures", "demo_case")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "fixtures", "demo_case")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "fixtures", "demo_case")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "fixtures", "demo_case")),
        ];

        return candidates.FirstOrDefault(Directory.Exists)
            ?? throw new DirectoryNotFoundException("Could not locate fixtures/demo_case");
    }
}
