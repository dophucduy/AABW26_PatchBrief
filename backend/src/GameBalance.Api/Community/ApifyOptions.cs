namespace GameBalance.Api.Community;

public sealed class ApifyOptions
{
    public const string SectionName = "Apify";

    public string ApiToken { get; init; } = string.Empty;

    public string ActorId { get; init; } =
        "automation-lab~steam-game-reviews-scraper";

    public string BaseUrl { get; init; } = "https://api.apify.com/";

    public int PollIntervalMilliseconds { get; init; } = 1000;

    public int RunTimeoutSeconds { get; init; } = 60;
}
