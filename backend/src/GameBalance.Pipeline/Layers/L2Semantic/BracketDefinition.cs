using System.Globalization;
using System.Text.Json;

namespace GameBalance.Pipeline.Layers.L2Semantic;

/// <summary>A skill cohort declared in game_definition.json.</summary>
public sealed record BracketDefinition(
    string Id,
    string? Label = null,
    double? MinRating = null,
    double? MaxRating = null)
{
    /// <summary>Read bracket definitions from the root "brackets" array.</summary>
    public static IReadOnlyList<BracketDefinition> ParseGameDefinition(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("brackets", out JsonElement brackets) ||
            brackets.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<BracketDefinition>();
        }

        var result = new List<BracketDefinition>();
        foreach (JsonElement item in brackets.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("id", out JsonElement idElement) ||
                idElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(idElement.GetString()))
            {
                continue;
            }

            result.Add(new BracketDefinition(
                idElement.GetString()!,
                ReadString(item, "label"),
                ReadNumber(item, "min_rating") ?? ReadNumber(item, "min"),
                ReadNumber(item, "max_rating") ?? ReadNumber(item, "max")));
        }

        return result;
    }

    public bool Contains(double rating) =>
        (!MinRating.HasValue || rating >= MinRating.Value) &&
        (!MaxRating.HasValue || rating <= MaxRating.Value);

    private static string? ReadString(JsonElement item, string name) =>
        item.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? ReadNumber(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String &&
               double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }
}
