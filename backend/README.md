# Backend

ASP.NET Core application and pipeline modules (L0-L7).

## Steam community feedback with Apify

Configure the API token as an environment variable. Never commit the token:

```powershell
$env:Apify__ApiToken = "your_apify_token"
dotnet run --project src\GameBalance.Api
```

Scrape and normalize Steam reviews by game name:

```http
POST /community/steam/scrape
Content-Type: application/json

{
  "game_name": "Strinova",
  "max_reviews": 100,
  "language": "english",
  "review_type": "all"
}
```

To enrich L4 during a normal multipart analysis, append these optional fields:

```text
community_game_name=Strinova
community_max_reviews=100
community_language=english
```

The API starts the configured Apify Actor with `searchQueries`, waits for the
run, reads its dataset, and maps reviews into `community.clusters`. Feedback
mentioning a known entity name/ID is assigned to that entity; other feedback is
preserved as a game-level cluster.
