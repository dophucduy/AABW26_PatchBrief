using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameBalance.Pipeline.Layers.L4Context;
using Microsoft.Extensions.Options;

namespace GameBalance.Api.Community;

public sealed class ApifyCommunityClient
{
    private static readonly HashSet<string> TerminalStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "SUCCEEDED", "FAILED", "ABORTED", "TIMED-OUT",
        };

    private readonly HttpClient _httpClient;
    private readonly ApifyOptions _options;

    public ApifyCommunityClient(HttpClient httpClient, IOptions<ApifyOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);
    }

    public async Task<ApifyCommunityResult> ScrapeAsync(
        CommunityScrapeRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.RunTimeoutSeconds));

        var input = new
        {
            searchQueries = new[] { request.GameName.Trim() },
            language = request.Language,
            reviewType = request.ReviewType,
            purchaseType = "all",
            sort = "recent",
            maxReviewsPerGame = request.MaxReviews,
            includeGameInfo = true,
        };

        using var startRequest = CreateRequest(
            HttpMethod.Post,
            $"v2/acts/{_options.ActorId}/runs");
        startRequest.Content = JsonContent.Create(input);
        using HttpResponseMessage startResponse = await _httpClient.SendAsync(
            startRequest,
            timeout.Token);
        await EnsureSuccessAsync(startResponse, timeout.Token);

        ApifyRunEnvelope? started = await startResponse.Content.ReadFromJsonAsync<ApifyRunEnvelope>(
            cancellationToken: timeout.Token);
        string runId = started?.Data?.Id
            ?? throw new InvalidDataException("Apify did not return a run ID");

        ApifyRunData run = await WaitForRunAsync(runId, timeout.Token);
        if (!run.Status.Equals("SUCCEEDED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Apify Actor run {runId} ended with status {run.Status}");
        }

        string datasetId = run.DefaultDatasetId
            ?? throw new InvalidDataException("Apify run did not return a dataset ID");
        IReadOnlyList<ApifySteamReview> reviews = await ReadDatasetAsync(
            datasetId,
            request.MaxReviews,
            timeout.Token);

        return new ApifyCommunityResult
        {
            RunId = runId,
            DatasetId = datasetId,
            GameName = reviews.FirstOrDefault()?.GameName ?? request.GameName.Trim(),
            Feedback = reviews
                .Where(review => !string.IsNullOrWhiteSpace(review.Review))
                .Select(review => new CommunityFeedbackItem(
                    "steam",
                    review.GameName ?? request.GameName.Trim(),
                    review.Review!,
                    review.Sentiment ?? (review.Positive ? "positive" : "negative"),
                    review.Language,
                    review.CreatedAt,
                    review.VotesUp))
                .ToList(),
        };
    }

    private async Task<ApifyRunData> WaitForRunAsync(
        string runId,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            using var request = CreateRequest(HttpMethod.Get, $"v2/actor-runs/{runId}");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            ApifyRunEnvelope? envelope = await response.Content.ReadFromJsonAsync<ApifyRunEnvelope>(
                cancellationToken: cancellationToken);
            ApifyRunData run = envelope?.Data
                ?? throw new InvalidDataException("Apify returned an invalid run response");

            if (TerminalStatuses.Contains(run.Status))
            {
                return run;
            }

            await Task.Delay(_options.PollIntervalMilliseconds, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<ApifySteamReview>> ReadDatasetAsync(
        string datasetId,
        int limit,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(
            HttpMethod.Get,
            $"v2/datasets/{datasetId}/items?clean=true&limit={limit}");
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<ApifySteamReview>>(
                   cancellationToken: cancellationToken)
               ?? new List<ApifySteamReview>();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
        return request;
    }

    private void Validate(CommunityScrapeRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            throw new InvalidOperationException(
                "Apify API token is not configured. Set Apify__ApiToken.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(request.GameName);
        if (request.MaxReviews is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.MaxReviews),
                "max_reviews must be between 1 and 1000");
        }
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Apify API returned {(int)response.StatusCode}: {body}");
    }

    private sealed class ApifyRunEnvelope
    {
        [JsonPropertyName("data")]
        public ApifyRunData? Data { get; init; }
    }

    private sealed class ApifyRunData
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("defaultDatasetId")]
        public string? DefaultDatasetId { get; init; }
    }

    private sealed class ApifySteamReview
    {
        [JsonPropertyName("gameName")]
        public string? GameName { get; init; }

        [JsonPropertyName("review")]
        public string? Review { get; init; }

        [JsonPropertyName("positive")]
        public bool Positive { get; init; }

        [JsonPropertyName("sentiment")]
        public string? Sentiment { get; init; }

        [JsonPropertyName("language")]
        public string? Language { get; init; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset? CreatedAt { get; init; }

        [JsonPropertyName("votesUp")]
        public int VotesUp { get; init; }
    }
}
