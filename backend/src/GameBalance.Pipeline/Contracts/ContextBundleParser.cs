using System.Text.Json;

namespace GameBalance.Pipeline.Contracts;

public static class ContextBundleParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ContextBundle Parse(string json)
    {
        ContextBundle? bundle = JsonSerializer.Deserialize<ContextBundle>(json, Options);
        if (bundle is null)
        {
            throw new InvalidDataException("context_bundle.json could not be parsed");
        }

        return bundle;
    }
}
