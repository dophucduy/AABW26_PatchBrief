using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameBalance.Pipeline.Layers.L3Metric;
using GameBalance.Pipeline.Contracts;
using GameBalance.Pipeline.Layers.L5Impact;
using GameBalance.Pipeline.Layers.L6Risk;

namespace GameBalance.Pipeline.Layers.L7Report;

/// <summary>L7 - generates the final insight report via LLM with template fallback.</summary>
public sealed class ReportGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<InsightReport> GenerateAsync(
        MetricResult metrics,
        ContextBundle context,
        ImpactResult impact,
        RiskResult risks,
        LlmOptions? options,
        HttpClient? httpClient,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options?.ApiKey) || httpClient is null)
        {
            return TemplateReportBuilder.Build(impact, risks, context);
        }

        try
        {
            InsightReport? llmReport = options.Provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase)
                ? await CallGeminiAsync(metrics, context, impact, risks, options, httpClient, cancellationToken)
                : await CallOpenAiAsync(metrics, context, impact, risks, options, httpClient, cancellationToken);

            return llmReport ?? TemplateReportBuilder.Build(impact, risks, context);
        }
        catch
        {
            return TemplateReportBuilder.Build(impact, risks, context);
        }
    }

    private static async Task<InsightReport?> CallOpenAiAsync(
        MetricResult metrics,
        ContextBundle context,
        ImpactResult impact,
        RiskResult risks,
        LlmOptions options,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        string payload = BuildPromptPayload(metrics, context, impact, risks);
        var requestBody = new
        {
            model = options.Model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = payload },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        string? content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return ParseLlmReport(content, impact, risks);
    }

    private static async Task<InsightReport?> CallGeminiAsync(
        MetricResult metrics,
        ContextBundle context,
        ImpactResult impact,
        RiskResult risks,
        LlmOptions options,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        string payload = BuildPromptPayload(metrics, context, impact, risks);
        string model = options.Model;
        string url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={options.ApiKey}";

        var requestBody = new
        {
            systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = payload } } },
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        string? content = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return ParseLlmReport(content, impact, risks);
    }

    private static string BuildPromptPayload(
        MetricResult metrics,
        ContextBundle context,
        ImpactResult impact,
        RiskResult risks) =>
        JsonSerializer.Serialize(new
        {
            metrics,
            context = new
            {
                proposed_changes = context.JoinedChanges,
                community = context.Community,
                version = context.UpdatePlan.Version,
            },
            impact,
            risks,
        }, JsonOptions);

    private static InsightReport? ParseLlmReport(string? content, ImpactResult impact, RiskResult risks)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        LlmReportPayload? payload = JsonSerializer.Deserialize<LlmReportPayload>(content, JsonOptions);
        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.ExecutiveSummary) ||
            string.IsNullOrWhiteSpace(payload.ReportMarkdown))
        {
            return null;
        }

        return new InsightReport
        {
            ExecutiveSummary = payload.ExecutiveSummary,
            WhoIsAffected = impact.WhoIsAffected,
            Alignment = impact.Alignment,
            Risks = risks.Risks,
            SolutionPaths = risks.SolutionPaths,
            ValidationPlan = risks.ValidationPlan,
            ReportMarkdown = payload.ReportMarkdown,
            DraftPlayerComms = payload.DraftPlayerComms,
            GeneratedBy = "llm",
        };
    }

    private sealed class LlmReportPayload
    {
        [JsonPropertyName("executive_summary")]
        public string? ExecutiveSummary { get; init; }

        [JsonPropertyName("report_markdown")]
        public string? ReportMarkdown { get; init; }

        [JsonPropertyName("draft_player_comms")]
        public string? DraftPlayerComms { get; init; }
    }

    private const string SystemPrompt = """
        You are a game balance analyst writing a patch decision brief.
        Rules:
        1. Only use data provided in the user JSON payload.
        2. Do not invent stats, quotes, or player counts.
        3. Every claim must reference evidence from impact.patterns or risks.
        4. Suggest paths as investigate / designer decides — never prescribe exact numeric changes.
        5. If intentional_difficulty or locked levers appear, note the constraint.
        Return JSON only with keys: executive_summary, report_markdown, draft_player_comms.
        """;
}
