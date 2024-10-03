using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Ocelot.Infrastructure;

public static class JsonSerializerOptionsFactory
{
    public static readonly JsonSerializerOptions Web = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public static readonly JsonSerializerOptions WebWriteIndented = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = true
    };

    public static List<string> ExtractValuesFromJsonPath(this JsonDocument document, string jsonPath)
    {
        var root = document.RootElement;

        var pathParts = jsonPath.Trim('$', '.').Replace("[*]", "").Trim().Split('.');

        var elements = new List<string>();

        TraverseJsonPath(root, pathParts, 0, elements);

        return elements;
    }

    private static void TraverseJsonPath(JsonElement currentElement, string[] pathParts, int index, List<string> elements)
    {
        if (index >= pathParts.Length) return;

        var part = pathParts[index];

        if (currentElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in currentElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    if (element.TryGetProperty(part, out JsonElement nextElement))
                    {
                        if (nextElement.ValueKind == JsonValueKind.Array)
                        {
                            TraverseJsonPath(nextElement, pathParts, 0, elements);
                        }
                        else
                        {
                            var item = nextElement.ToString();
                            if (!string.IsNullOrWhiteSpace(item))
                                elements.Add(item);
                        }
                    }
                    else
                    {
                        TraverseJsonPath(element, pathParts, index + 1, elements);
                    }
                }
                else
                {
                    TraverseJsonPath(element, pathParts, index + 1, elements);
                }
            }
        }
        else if (currentElement.ValueKind == JsonValueKind.Object)
        {
            if (currentElement.TryGetProperty(part, out JsonElement nextElement))
            {
                if (nextElement.ValueKind == JsonValueKind.Array)
                {
                    TraverseJsonPath(nextElement, pathParts, index + 1, elements);
                }
                else
                {
                    var item = nextElement.ToString();
                    if (!string.IsNullOrWhiteSpace(item))
                        elements.Add(item);
                }
            }
        }
        else
        {
            var item = currentElement.ToString();
            if (!string.IsNullOrWhiteSpace(item))
                elements.Add(item);
        }
    }
}
