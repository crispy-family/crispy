using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Crispy.Application.DTOs;
using Crispy.Application.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Crispy.Application.Services
{
    public class RecipeImportService : IRecipeImportService
    {
        private static readonly HashSet<string> KnownUnits = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "г", "гр", "грам", "грамів", "кг", "мл", "л",
            "ст.л", "ст.л.", "ч.л", "ч.л.", "шт", "шт.",
            "пучок", "пучки"
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<RecipeImportService> _logger;

        public RecipeImportService(HttpClient httpClient, ILogger<RecipeImportService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CrispyBot/1.0 (+https://crispy.local)");
            }
        }

        public async Task<ImportedRecipeDto?> ImportAsync(string url, CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return null;
            }

            string html;
            try
            {
                html = await _httpClient.GetStringAsync(uri, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load recipe page from {Url}", url);
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var imported = TryParseJsonLd(doc);
            if (imported == null)
            {
                imported = new ImportedRecipeDto
                {
                    Title = GetMetaContent(doc, "property", "og:title") ?? GetTitle(doc),
                    ImageUrl = GetMetaContent(doc, "property", "og:image"),
                    Description = GetMetaContent(doc, "name", "description") ?? string.Empty
                };
            }

            imported.SourceUrl = uri.ToString();

            if (string.IsNullOrWhiteSpace(imported.Title))
            {
                return null;
            }

            imported.Title = WebUtility.HtmlDecode(imported.Title).Trim();
            imported.Description = WebUtility.HtmlDecode(imported.Description).Trim();
            imported.ImageUrl = imported.ImageUrl?.Trim();

            return imported;
        }

        private static ImportedRecipeDto? TryParseJsonLd(HtmlDocument doc)
        {
            var scripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (scripts == null)
            {
                return null;
            }

            foreach (var script in scripts)
            {
                if (string.IsNullOrWhiteSpace(script.InnerText))
                {
                    continue;
                }

                JsonNode? node;
                try
                {
                    node = JsonNode.Parse(script.InnerText);
                }
                catch
                {
                    continue;
                }

                foreach (var obj in EnumerateObjects(node))
                {
                    if (!IsRecipeType(obj["@type"]))
                    {
                        continue;
                    }

                    var title = obj["name"]?.GetValue<string>() ?? string.Empty;
                    var description = ExtractInstructions(obj["recipeInstructions"])
                        ?? obj["description"]?.GetValue<string>()
                        ?? string.Empty;

                    var ingredients = ExtractIngredients(obj["recipeIngredient"])
                        .Select(ParseIngredient)
                        .Where(i => i != null)
                        .Select(i => i!)
                        .ToList();

                    return new ImportedRecipeDto
                    {
                        Title = title,
                        Description = description,
                        ImageUrl = ExtractImageUrl(obj["image"]),
                        Ingredients = ingredients
                    };
                }
            }

            return null;
        }

        private static IEnumerable<JsonObject> EnumerateObjects(JsonNode? node)
        {
            if (node == null)
            {
                yield break;
            }

            if (node is JsonObject obj)
            {
                yield return obj;

                if (obj.TryGetPropertyValue("@graph", out var graphNode))
                {
                    foreach (var graphObj in EnumerateObjects(graphNode))
                    {
                        yield return graphObj;
                    }
                }
            }

            if (node is JsonArray array)
            {
                foreach (var item in array)
                {
                    foreach (var child in EnumerateObjects(item))
                    {
                        yield return child;
                    }
                }
            }
        }

        private static bool IsRecipeType(JsonNode? typeNode)
        {
            if (typeNode is JsonValue value)
            {
                return string.Equals(value.GetValue<string>(), "Recipe", StringComparison.OrdinalIgnoreCase);
            }

            if (typeNode is JsonArray array)
            {
                return array.Any(IsRecipeType);
            }

            return false;
        }

        private static string? ExtractImageUrl(JsonNode? imageNode)
        {
            if (imageNode is JsonValue value)
            {
                return value.GetValue<string>();
            }

            if (imageNode is JsonObject obj)
            {
                return obj["url"]?.GetValue<string>();
            }

            if (imageNode is JsonArray array)
            {
                foreach (var item in array)
                {
                    var url = ExtractImageUrl(item);
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }
            }

            return null;
        }

        private static string? ExtractInstructions(JsonNode? instructionsNode)
        {
            if (instructionsNode == null)
            {
                return null;
            }

            if (instructionsNode is JsonValue value)
            {
                return value.GetValue<string>();
            }

            if (instructionsNode is JsonObject obj)
            {
                return obj["text"]?.GetValue<string>();
            }

            if (instructionsNode is JsonArray array)
            {
                var steps = new List<string>();

                foreach (var item in array)
                {
                    if (item is JsonValue textValue)
                    {
                        steps.Add(textValue.GetValue<string>());
                        continue;
                    }

                    if (item is JsonObject stepObj && stepObj["text"] is JsonValue stepText)
                    {
                        steps.Add(stepText.GetValue<string>());
                    }
                }

                return steps.Count > 0 ? string.Join(Environment.NewLine, steps) : null;
            }

            return null;
        }

        private static List<string> ExtractIngredients(JsonNode? ingredientsNode)
        {
            var result = new List<string>();

            if (ingredientsNode == null)
            {
                return result;
            }

            if (ingredientsNode is JsonValue value)
            {
                var text = value.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    result.Add(text);
                }

                return result;
            }

            if (ingredientsNode is JsonArray array)
            {
                foreach (var item in array)
                {
                    if (item is JsonValue itemValue)
                    {
                        var text = itemValue.GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            result.Add(text);
                        }

                        continue;
                    }

                    if (item is JsonObject obj && obj["text"] is JsonValue textValue)
                    {
                        var text = textValue.GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            result.Add(text);
                        }
                    }
                }
            }

            return result;
        }

        private static RecipeIngredientDto? ParseIngredient(string raw)
        {
            var text = WebUtility.HtmlDecode(raw).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            text = Regex.Replace(text, @"(?<=\d)(?=[^\d\s.,/])", " ");

            if (TryParseParenthesized(text, out var name, out var quantity, out var unit))
            {
                return new RecipeIngredientDto
                {
                    Name = name,
                    Quantity = quantity,
                    Unit = unit
                };
            }

            if (TryParseLeading(text, out name, out quantity, out unit))
            {
                return new RecipeIngredientDto
                {
                    Name = name,
                    Quantity = quantity,
                    Unit = unit
                };
            }

            return new RecipeIngredientDto
            {
                Name = text,
                Quantity = 1,
                Unit = "шт"
            };
        }

        private static bool TryParseParenthesized(string text, out string name, out float quantity, out string unit)
        {
            name = string.Empty;
            quantity = 0;
            unit = "шт";

            var match = Regex.Match(text, @"^(?<name>.*?)\s*\((?<qty>[\d.,/ ]+)\s*(?<unit>[^\)]+)\)\s*$");
            if (!match.Success)
            {
                return false;
            }

            if (!TryParseQuantityText(match.Groups["qty"].Value, out quantity))
            {
                return false;
            }

            name = match.Groups["name"].Value.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = text;
            }

            unit = match.Groups["unit"].Value.Trim();
            if (string.IsNullOrWhiteSpace(unit))
            {
                unit = "шт";
            }

            return true;
        }

        private static bool TryParseLeading(string text, out string name, out float quantity, out string unit)
        {
            name = string.Empty;
            quantity = 0;
            unit = "шт";

            var match = Regex.Match(text, @"^(?<qty>[\d.,/]+(?:\s+[\d/]+)?)\s*(?<unit>[^\s]+)?\s*(?<name>.+)$");
            if (!match.Success)
            {
                return false;
            }

            if (!TryParseQuantityText(match.Groups["qty"].Value, out quantity))
            {
                return false;
            }

            var unitToken = match.Groups["unit"].Value.Trim();
            var tailName = match.Groups["name"].Value.Trim();

            if (!string.IsNullOrWhiteSpace(unitToken) && IsKnownUnit(unitToken))
            {
                unit = unitToken;
                name = tailName;
            }
            else
            {
                unit = "шт";
                name = string.IsNullOrWhiteSpace(unitToken) ? tailName : $"{unitToken} {tailName}".Trim();
            }

            return true;
        }

        private static bool TryParseQuantityText(string text, out float value)
        {
            value = 0;
            var normalized = text.Trim();

            if (TryParseTokenQuantity(normalized, out value))
            {
                return true;
            }

            var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 &&
                TryParseTokenQuantity(parts[0], out var first) &&
                TryParseTokenQuantity(parts[1], out var second))
            {
                value = first + second;
                return true;
            }

            return false;
        }

        private static bool TryParseTokenQuantity(string token, out float value)
        {
            value = 0;
            var normalized = token.Replace(',', '.');

            if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }

            if (normalized.Contains('/'))
            {
                var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 &&
                    float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator) &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator) &&
                    denominator > 0)
                {
                    value = numerator / denominator;
                    return true;
                }
            }

            return false;
        }

        private static bool IsKnownUnit(string unit)
        {
            var normalized = unit.Trim().Trim('.', ',', ';').ToLowerInvariant();
            return KnownUnits.Contains(normalized);
        }

        private static string? GetMetaContent(HtmlDocument doc, string attrName, string attrValue)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//meta[@{attrName}='{attrValue}']");
            return node?.GetAttributeValue("content", null);
        }

        private static string GetTitle(HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode("//title");
            return node?.InnerText ?? string.Empty;
        }
    }
}