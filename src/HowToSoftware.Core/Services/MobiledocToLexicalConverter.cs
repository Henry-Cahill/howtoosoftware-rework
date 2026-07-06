using System.Text.Json;
using System.Text.Json.Nodes;

namespace HowToSoftware.Core.Services;

/// <summary>
/// Converts Ghost Mobiledoc 0.3.1 JSON to Lexical editor JSON format.
/// </summary>
public static class MobiledocToLexicalConverter
{
    public static string? Convert(string? mobiledocJson)
    {
        if (string.IsNullOrWhiteSpace(mobiledocJson))
            return null;

        using var doc = JsonDocument.Parse(mobiledocJson);
        var root = doc.RootElement;

        var markups = ParseMarkups(root);
        var atoms = ParseAtoms(root);
        var cards = ParseCards(root);

        if (!root.TryGetProperty("sections", out var sections))
            return null;

        var children = new JsonArray();

        foreach (var section in sections.EnumerateArray())
        {
            var node = ConvertSection(section, markups, atoms, cards);
            if (node is not null)
                children.Add(node);
        }

        // If no content was generated, return null
        if (children.Count == 0)
            return null;

        var lexical = new JsonObject
        {
            ["root"] = new JsonObject
            {
                ["children"] = children,
                ["direction"] = "ltr",
                ["format"] = "",
                ["indent"] = 0,
                ["type"] = "root",
                ["version"] = 1,
            }
        };

        return lexical.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    // ─── Mobiledoc Parsing ─────────────────────────────────────

    private sealed record Markup(string TagName, List<KeyValuePair<string, string>> Attributes);
    private sealed record Atom(string Name, string Value, JsonElement Payload);
    private sealed record Card(string Name, JsonElement Payload);

    private static List<Markup> ParseMarkups(JsonElement root)
    {
        var markups = new List<Markup>();
        if (!root.TryGetProperty("markups", out var arr)) return markups;

        foreach (var m in arr.EnumerateArray())
        {
            var items = m.EnumerateArray().ToArray();
            var tagName = items[0].GetString() ?? "";
            var attrs = new List<KeyValuePair<string, string>>();
            if (items.Length > 1 && items[1].ValueKind == JsonValueKind.Array)
            {
                var a = items[1].EnumerateArray().ToArray();
                for (int i = 0; i + 1 < a.Length; i += 2)
                    attrs.Add(new(a[i].GetString() ?? "", a[i + 1].GetString() ?? ""));
            }
            markups.Add(new Markup(tagName, attrs));
        }
        return markups;
    }

    private static List<Atom> ParseAtoms(JsonElement root)
    {
        var atoms = new List<Atom>();
        if (!root.TryGetProperty("atoms", out var arr)) return atoms;

        foreach (var a in arr.EnumerateArray())
        {
            var items = a.EnumerateArray().ToArray();
            var name = items[0].GetString() ?? "";
            var value = items.Length > 1 ? items[1].GetString() ?? "" : "";
            var payload = items.Length > 2 ? items[2] : default;
            atoms.Add(new Atom(name, value, payload));
        }
        return atoms;
    }

    private static List<Card> ParseCards(JsonElement root)
    {
        var cards = new List<Card>();
        if (!root.TryGetProperty("cards", out var arr)) return cards;

        foreach (var c in arr.EnumerateArray())
        {
            var items = c.EnumerateArray().ToArray();
            var name = items[0].GetString() ?? "";
            var payload = items.Length > 1 ? items[1] : default;
            cards.Add(new Card(name, payload));
        }
        return cards;
    }

    // ─── Section → Lexical Node Conversion ─────────────────────

    private static JsonNode? ConvertSection(
        JsonElement section, List<Markup> markups, List<Atom> atoms, List<Card> cards)
    {
        var items = section.EnumerateArray().ToArray();
        if (items.Length == 0) return null;

        return items[0].GetInt32() switch
        {
            1 => ConvertMarkupSection(items, markups, atoms),
            2 => ConvertCardSection(items, cards),
            3 => ConvertListSection(items, markups, atoms),
            10 => ConvertImageSection(items),
            _ => null,
        };
    }

    private static JsonNode? ConvertMarkupSection(
        JsonElement[] items, List<Markup> markups, List<Atom> atoms)
    {
        if (items.Length < 3) return null;

        var tagName = (items[1].GetString() ?? "p").ToLowerInvariant();
        var children = ConvertMarkers(items[2], markups, atoms);

        if (tagName is "h1" or "h2" or "h3" or "h4" or "h5" or "h6")
        {
            return MakeHeading(tagName, children);
        }

        if (tagName is "blockquote")
        {
            return MakeQuote(children);
        }

        return MakeParagraph(children);
    }

    private static JsonNode? ConvertCardSection(JsonElement[] items, List<Card> cards)
    {
        if (items.Length < 2) return null;
        var idx = items[1].GetInt32();
        if (idx < 0 || idx >= cards.Count) return null;
        return ConvertCard(cards[idx]);
    }

    private static JsonNode? ConvertListSection(
        JsonElement[] items, List<Markup> markups, List<Atom> atoms)
    {
        if (items.Length < 3) return null;

        var listTag = (items[1].GetString() ?? "ul").ToLowerInvariant();
        var listType = listTag == "ol" ? "number" : "bullet";

        var listItems = new JsonArray();
        foreach (var li in items[2].EnumerateArray())
        {
            var liChildren = ConvertMarkers(li, markups, atoms);
            listItems.Add(new JsonObject
            {
                ["children"] = liChildren,
                ["direction"] = "ltr",
                ["format"] = "",
                ["indent"] = 0,
                ["type"] = "listitem",
                ["version"] = 1,
                ["value"] = 1,
            });
        }

        return new JsonObject
        {
            ["children"] = listItems,
            ["direction"] = "ltr",
            ["format"] = "",
            ["indent"] = 0,
            ["type"] = "list",
            ["listType"] = listType,
            ["start"] = 1,
            ["tag"] = listTag,
            ["version"] = 1,
        };
    }

    private static JsonNode? ConvertImageSection(JsonElement[] items)
    {
        if (items.Length < 2) return null;
        var src = items[1].GetString() ?? "";
        return MakeImage(src, "", "", "");
    }

    // ─── Marker → Lexical Text/Link Conversion ────────────────

    private static JsonArray ConvertMarkers(
        JsonElement markersArray, List<Markup> markups, List<Atom> atoms)
    {
        var result = new JsonArray();
        var openStack = new Stack<Markup>();

        // Track currently-open link markup so we can wrap text in link nodes
        string? currentLinkUrl = null;
        var linkChildren = new JsonArray();

        foreach (var marker in markersArray.EnumerateArray())
        {
            var parts = marker.EnumerateArray().ToArray();
            if (parts.Length < 4) continue;

            var textType = parts[0].GetInt32();
            var openIndices = parts[1];
            var closeCount = parts[2].GetInt32();

            // Process open markups
            foreach (var idx in openIndices.EnumerateArray())
            {
                var mi = idx.GetInt32();
                if (mi >= 0 && mi < markups.Count)
                {
                    var markup = markups[mi];
                    openStack.Push(markup);

                    if (markup.TagName.Equals("a", StringComparison.OrdinalIgnoreCase))
                    {
                        var href = markup.Attributes
                            .FirstOrDefault(a => a.Key.Equals("href", StringComparison.OrdinalIgnoreCase));
                        currentLinkUrl = href.Value ?? "";
                        linkChildren = [];
                    }
                }
            }

            // Compute text format from open markups (excluding <a>)
            int format = 0;
            foreach (var m in openStack)
            {
                format |= TagToFormat(m.TagName);
            }

            // Create the text or atom node
            JsonNode? textNode = null;
            if (textType == 0)
            {
                var text = parts[3].GetString() ?? "";
                textNode = MakeText(text, format);
            }
            else if (textType == 1)
            {
                var atomIndex = parts[3].GetInt32();
                if (atomIndex >= 0 && atomIndex < atoms.Count)
                {
                    var atom = atoms[atomIndex];
                    textNode = atom.Name switch
                    {
                        "soft-return" => MakeLineBreak(),
                        _ => MakeText(atom.Value, format),
                    };
                }
            }

            // Append to link children or directly to result
            if (textNode is not null)
            {
                if (currentLinkUrl is not null)
                    linkChildren.Add(textNode);
                else
                    result.Add(textNode);
            }

            // Process close markups
            for (int i = 0; i < closeCount && openStack.Count > 0; i++)
            {
                var closed = openStack.Pop();
                if (closed.TagName.Equals("a", StringComparison.OrdinalIgnoreCase) && currentLinkUrl is not null)
                {
                    result.Add(MakeLink(currentLinkUrl, linkChildren));
                    currentLinkUrl = null;
                    linkChildren = [];
                }
            }
        }

        // Flush any unclosed link
        if (currentLinkUrl is not null && linkChildren.Count > 0)
        {
            result.Add(MakeLink(currentLinkUrl, linkChildren));
        }

        return result;
    }

    // ─── Card Conversion ───────────────────────────────────────

    private static JsonNode? ConvertCard(Card card)
    {
        return card.Name switch
        {
            "hr" => MakeHorizontalRule(),
            "image" => ConvertImageCard(card),
            "code" => ConvertCodeCard(card),
            "html" => ConvertHtmlCard(card),
            "embed" => ConvertEmbedCard(card),
            "bookmark" => ConvertBookmarkCard(card),
            "markdown" => ConvertMarkdownCard(card),
            _ => null,
        };
    }

    private static JsonNode? ConvertImageCard(Card card)
    {
        var src = GetPayload(card.Payload, "src");
        if (string.IsNullOrEmpty(src)) return null;

        return MakeImage(
            src,
            GetPayload(card.Payload, "alt") ?? "",
            GetPayload(card.Payload, "caption") ?? "",
            GetPayload(card.Payload, "cardWidth") ?? "");
    }

    private static JsonNode? ConvertCodeCard(Card card)
    {
        var code = GetPayload(card.Payload, "code");
        if (string.IsNullOrEmpty(code)) return null;

        return new JsonObject
        {
            ["type"] = "codeblock",
            ["code"] = code,
            ["language"] = GetPayload(card.Payload, "language") ?? "",
            ["version"] = 1,
        };
    }

    private static JsonNode? ConvertHtmlCard(Card card)
    {
        var html = GetPayload(card.Payload, "html");
        if (string.IsNullOrEmpty(html)) return null;

        return new JsonObject
        {
            ["type"] = "html",
            ["html"] = html,
            ["version"] = 1,
        };
    }

    private static JsonNode? ConvertEmbedCard(Card card)
    {
        var html = GetPayload(card.Payload, "html");
        var url = GetPayload(card.Payload, "url") ?? "";

        return new JsonObject
        {
            ["type"] = "embed",
            ["url"] = url,
            ["html"] = html ?? "",
            ["version"] = 1,
        };
    }

    private static JsonNode? ConvertBookmarkCard(Card card)
    {
        var url = GetPayload(card.Payload, "url");
        if (string.IsNullOrEmpty(url)) return null;

        return new JsonObject
        {
            ["type"] = "bookmark",
            ["url"] = url,
            ["metadata"] = new JsonObject
            {
                ["title"] = GetPayload(card.Payload, "title") ?? "",
                ["description"] = GetPayload(card.Payload, "description") ?? "",
                ["icon"] = GetPayload(card.Payload, "icon") ?? "",
                ["thumbnail"] = GetPayload(card.Payload, "thumbnail") ?? "",
                ["publisher"] = GetPayload(card.Payload, "publisher") ?? "",
            },
            ["version"] = 1,
        };
    }

    private static JsonNode? ConvertMarkdownCard(Card card)
    {
        var md = GetPayload(card.Payload, "markdown");
        if (string.IsNullOrEmpty(md)) return null;

        // Store as HTML card since we can't render markdown in Lexical directly
        return new JsonObject
        {
            ["type"] = "html",
            ["html"] = md,
            ["version"] = 1,
        };
    }

    // ─── Lexical Node Builders ─────────────────────────────────

    private static JsonObject MakeParagraph(JsonArray children) => new()
    {
        ["children"] = children,
        ["direction"] = "ltr",
        ["format"] = "",
        ["indent"] = 0,
        ["type"] = "paragraph",
        ["version"] = 1,
    };

    private static JsonObject MakeHeading(string tag, JsonArray children) => new()
    {
        ["children"] = children,
        ["direction"] = "ltr",
        ["format"] = "",
        ["indent"] = 0,
        ["type"] = "heading",
        ["tag"] = tag,
        ["version"] = 1,
    };

    private static JsonObject MakeQuote(JsonArray children) => new()
    {
        ["children"] = children,
        ["direction"] = "ltr",
        ["format"] = "",
        ["indent"] = 0,
        ["type"] = "quote",
        ["version"] = 1,
    };

    private static JsonObject MakeText(string text, int format) => new()
    {
        ["detail"] = 0,
        ["format"] = format,
        ["mode"] = "normal",
        ["style"] = "",
        ["text"] = text,
        ["type"] = "text",
        ["version"] = 1,
    };

    private static JsonObject MakeLineBreak() => new()
    {
        ["type"] = "linebreak",
        ["version"] = 1,
    };

    private static JsonObject MakeLink(string url, JsonArray children) => new()
    {
        ["children"] = children,
        ["direction"] = "ltr",
        ["format"] = "",
        ["indent"] = 0,
        ["type"] = "link",
        ["rel"] = "noopener",
        ["target"] = (JsonNode?)null,
        ["title"] = "",
        ["url"] = url,
        ["version"] = 1,
    };

    private static JsonObject MakeHorizontalRule() => new()
    {
        ["type"] = "horizontalrule",
        ["version"] = 1,
    };

    private static JsonObject MakeImage(string src, string alt, string caption, string cardWidth) => new()
    {
        ["type"] = "image",
        ["src"] = src,
        ["altText"] = alt,
        ["caption"] = caption,
        ["cardWidth"] = cardWidth,
        ["width"] = 0,
        ["height"] = 0,
        ["version"] = 1,
    };

    // ─── Helpers ───────────────────────────────────────────────

    private static int TagToFormat(string tag) => tag.ToLowerInvariant() switch
    {
        "strong" or "b" => 1,     // Bold
        "em" or "i" => 2,         // Italic
        "s" or "del" or "strike" => 4, // Strikethrough
        "u" => 8,                 // Underline
        "code" => 16,             // Code
        "sub" => 32,              // Subscript
        "sup" => 64,              // Superscript
        _ => 0,
    };

    private static string? GetPayload(JsonElement payload, string prop)
    {
        if (payload.ValueKind == JsonValueKind.Undefined) return null;
        return payload.TryGetProperty(prop, out var v) ? v.GetString() : null;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
