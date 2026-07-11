using System.Text.Json;

namespace GameBalance.Api;

internal static class JsonUploadParser
{
    public static async Task<IReadOnlyList<Dictionary<string, object?>>> ParseEventsAsync(
        IFormFile file,
        string fieldName,
        CancellationToken cancellationToken)
    {
        string json = await ReadTextAsync(file, cancellationToken);
        try
        {
            if (IsJsonLines(file))
            {
                return ParseJsonLines(json);
            }

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement events = document.RootElement;
            if (events.ValueKind == JsonValueKind.Object &&
                events.TryGetProperty("events", out JsonElement wrapped))
            {
                events = wrapped;
            }

            if (events.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException("expected a JSON array or an object containing an 'events' array");
            }

            var result = new List<Dictionary<string, object?>>();
            foreach (JsonElement item in events.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException($"event at index {result.Count} is not a JSON object");
                }

                result.Add(ConvertObject(item));
            }

            return result;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"invalid JSON in '{fieldName}': {exception.Message}",
                exception);
        }
    }

    public static async Task<string> ReadTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using Stream stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public static async Task<IReadOnlyList<Dictionary<string, object?>>> ParseEventsFromFileAsync(
        string path,
        CancellationToken cancellationToken)
    {
        string json = await File.ReadAllTextAsync(path, cancellationToken);
        string fieldName = Path.GetFileName(path);
        try
        {
            if (Path.GetExtension(path).Equals(".jsonl", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(path).Equals(".ndjson", StringComparison.OrdinalIgnoreCase))
            {
                return ParseJsonLines(json);
            }

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement events = document.RootElement;
            if (events.ValueKind == JsonValueKind.Object &&
                events.TryGetProperty("events", out JsonElement wrapped))
            {
                events = wrapped;
            }

            if (events.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException("expected a JSON array or an object containing an 'events' array");
            }

            var result = new List<Dictionary<string, object?>>();
            foreach (JsonElement item in events.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException($"event at index {result.Count} is not a JSON object");
                }

                result.Add(ConvertObject(item));
            }

            return result;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"invalid JSON in '{fieldName}': {exception.Message}",
                exception);
        }
    }

    private static Dictionary<string, object?> ConvertObject(JsonElement element) =>
        element.EnumerateObject().ToDictionary(
            property => property.Name,
            property => ConvertValue(property.Value));

    private static bool IsJsonLines(IFormFile file) =>
        Path.GetExtension(file.FileName).Equals(".jsonl", StringComparison.OrdinalIgnoreCase) ||
        Path.GetExtension(file.FileName).Equals(".ndjson", StringComparison.OrdinalIgnoreCase) ||
        file.ContentType.Equals("application/x-ndjson", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<Dictionary<string, object?>> ParseJsonLines(string json)
    {
        var result = new List<Dictionary<string, object?>>();
        string[] lines = json.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (int lineNumber = 1; lineNumber <= lines.Length; lineNumber++)
        {
            string line = lines[lineNumber - 1];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using JsonDocument document = JsonDocument.Parse(line);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException($"JSONL line {lineNumber} is not a JSON object");
            }

            result.Add(ConvertObject(document.RootElement));
        }

        return result;
    }

    private static object? ConvertValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out long integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.Object => ConvertObject(value),
        JsonValueKind.Array => value.EnumerateArray().Select(ConvertValue).ToList(),
        _ => value.ToString(),
    };
}
