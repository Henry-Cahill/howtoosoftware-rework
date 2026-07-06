using System.Net;
using System.Text;
using System.Text.Json;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Services;

/// <summary>
/// Renders Mobiledoc 0.3.1 JSON to HTML.
/// Supports markup sections (p, h1-h6, blockquote, aside, pull-quote),
/// list sections (ul, ol), card sections, atoms, and nested markups.
/// </summary>
public sealed class MobiledocRenderer : IMobiledocRenderer
{
    public string Render(string mobiledocJson)
    {
        if (string.IsNullOrWhiteSpace(mobiledocJson))
            return string.Empty;

        using var doc = JsonDocument.Parse(mobiledocJson);
        var root = doc.RootElement;

        var markups = ParseMarkups(root);
        var atoms = ParseAtoms(root);
        var cards = ParseCards(root);

        if (!root.TryGetProperty("sections", out var sections))
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var section in sections.EnumerateArray())
        {
            RenderSection(section, markups, atoms, cards, sb);
        }

        return sb.ToString();
    }

    private static List<Markup> ParseMarkups(JsonElement root)
    {
        var markups = new List<Markup>();
        if (!root.TryGetProperty("markups", out var markupsArray))
            return markups;

        foreach (var m in markupsArray.EnumerateArray())
        {
            var items = m.EnumerateArray().ToArray();
            var tagName = items[0].GetString() ?? string.Empty;
            var attributes = new List<KeyValuePair<string, string>>();

            if (items.Length > 1 && items[1].ValueKind == JsonValueKind.Array)
            {
                var attrs = items[1].EnumerateArray().ToArray();
                for (int i = 0; i + 1 < attrs.Length; i += 2)
                {
                    attributes.Add(new KeyValuePair<string, string>(
                        attrs[i].GetString() ?? string.Empty,
                        attrs[i + 1].GetString() ?? string.Empty));
                }
            }

            markups.Add(new Markup(tagName, attributes));
        }

        return markups;
    }

    private static List<Atom> ParseAtoms(JsonElement root)
    {
        var atoms = new List<Atom>();
        if (!root.TryGetProperty("atoms", out var atomsArray))
            return atoms;

        foreach (var a in atomsArray.EnumerateArray())
        {
            var items = a.EnumerateArray().ToArray();
            var name = items[0].GetString() ?? string.Empty;
            var value = items.Length > 1 ? items[1].GetString() ?? string.Empty : string.Empty;
            var payload = items.Length > 2 ? items[2] : default;
            atoms.Add(new Atom(name, value, payload));
        }

        return atoms;
    }

    private static List<Card> ParseCards(JsonElement root)
    {
        var cards = new List<Card>();
        if (!root.TryGetProperty("cards", out var cardsArray))
            return cards;

        foreach (var c in cardsArray.EnumerateArray())
        {
            var items = c.EnumerateArray().ToArray();
            var name = items[0].GetString() ?? string.Empty;
            var payload = items.Length > 1 ? items[1] : default;
            cards.Add(new Card(name, payload));
        }

        return cards;
    }

    private static void RenderSection(
        JsonElement section,
        List<Markup> markups,
        List<Atom> atoms,
        List<Card> cards,
        StringBuilder sb)
    {
        var items = section.EnumerateArray().ToArray();
        if (items.Length == 0) return;

        var sectionType = items[0].GetInt32();

        switch (sectionType)
        {
            case 1: // Markup section: [1, tagName, markers]
                RenderMarkupSection(items, markups, atoms, sb);
                break;
            case 2: // Card section: [2, cardIndex]
                RenderCardSection(items, cards, sb);
                break;
            case 3: // List section: [3, tagName, [listItemMarkers...]]
                RenderListSection(items, markups, atoms, sb);
                break;
            case 10: // Image section (legacy): [10, src]
                RenderImageSection(items, sb);
                break;
        }
    }

    private static void RenderMarkupSection(
        JsonElement[] items,
        List<Markup> markups,
        List<Atom> atoms,
        StringBuilder sb)
    {
        if (items.Length < 3) return;

        var tagName = items[1].GetString() ?? "p";
        sb.Append('<').Append(Encode(tagName)).Append('>');
        RenderMarkers(items[2], markups, atoms, sb);
        sb.Append("</").Append(Encode(tagName)).Append('>');
    }

    private static void RenderCardSection(JsonElement[] items, List<Card> cards, StringBuilder sb)
    {
        if (items.Length < 2) return;

        var cardIndex = items[1].GetInt32();
        if (cardIndex < 0 || cardIndex >= cards.Count) return;

        var card = cards[cardIndex];
        RenderCard(card, sb);
    }

    private static void RenderListSection(
        JsonElement[] items,
        List<Markup> markups,
        List<Atom> atoms,
        StringBuilder sb)
    {
        if (items.Length < 3) return;

        // [3, tagName, [[markers for li1], [markers for li2], ...]]
        var listTag = items[1].GetString() ?? "ul";
        sb.Append('<').Append(Encode(listTag)).Append('>');

        foreach (var listItem in items[2].EnumerateArray())
        {
            sb.Append("<li>");
            RenderMarkers(listItem, markups, atoms, sb);
            sb.Append("</li>");
        }

        sb.Append("</").Append(Encode(listTag)).Append('>');
    }

    private static void RenderImageSection(JsonElement[] items, StringBuilder sb)
    {
        if (items.Length < 2) return;
        var src = items[1].GetString() ?? string.Empty;
        sb.Append("<img src=\"").Append(Encode(src)).Append("\" />");
    }

    private static void RenderMarkers(
        JsonElement markersArray,
        List<Markup> markups,
        List<Atom> atoms,
        StringBuilder sb)
    {
        // Each marker: [textType, openMarkupIndices, closeCount, value]
        // textType 0 = text, textType 1 = atom
        var openMarkupStack = new Stack<string>();

        foreach (var marker in markersArray.EnumerateArray())
        {
            var parts = marker.EnumerateArray().ToArray();
            if (parts.Length < 4) continue;

            var textType = parts[0].GetInt32();
            var openMarkupIndices = parts[1];
            var closeCount = parts[2].GetInt32();

            // Open markups
            foreach (var idx in openMarkupIndices.EnumerateArray())
            {
                var markupIndex = idx.GetInt32();
                if (markupIndex >= 0 && markupIndex < markups.Count)
                {
                    var markup = markups[markupIndex];
                    sb.Append('<').Append(Encode(markup.TagName));
                    foreach (var attr in markup.Attributes)
                    {
                        sb.Append(' ')
                          .Append(Encode(attr.Key))
                          .Append("=\"")
                          .Append(Encode(attr.Value))
                          .Append('"');
                    }
                    sb.Append('>');
                    openMarkupStack.Push(markup.TagName);
                }
            }

            // Render value
            if (textType == 0)
            {
                // Text marker
                var text = parts[3].GetString() ?? string.Empty;
                sb.Append(Encode(text));
            }
            else if (textType == 1)
            {
                // Atom marker — value is the atom index
                var atomIndex = parts[3].GetInt32();
                if (atomIndex >= 0 && atomIndex < atoms.Count)
                {
                    RenderAtom(atoms[atomIndex], sb);
                }
            }

            // Close markups
            for (int i = 0; i < closeCount && openMarkupStack.Count > 0; i++)
            {
                var tag = openMarkupStack.Pop();
                sb.Append("</").Append(Encode(tag)).Append('>');
            }
        }
    }

    private static void RenderAtom(Atom atom, StringBuilder sb)
    {
        switch (atom.Name)
        {
            case "soft-return":
                sb.Append("<br />");
                break;
            default:
                sb.Append(Encode(atom.Value));
                break;
        }
    }

    private static void RenderCard(Card card, StringBuilder sb)
    {
        switch (card.Name)
        {
            case "html":
                RenderHtmlCard(card, sb);
                break;
            case "image":
                RenderImageCard(card, sb);
                break;
            case "markdown":
                RenderMarkdownCard(card, sb);
                break;
            case "code":
                RenderCodeCard(card, sb);
                break;
            case "embed":
                RenderEmbedCard(card, sb);
                break;
            case "bookmark":
                RenderBookmarkCard(card, sb);
                break;
            case "gallery":
                RenderGalleryCard(card, sb);
                break;
            case "hr":
                sb.Append("<hr />");
                break;
            default:
                // Unknown card — skip
                break;
        }
    }

    private static void RenderHtmlCard(Card card, StringBuilder sb)
    {
        var html = GetPayloadString(card.Payload, "html");
        if (!string.IsNullOrEmpty(html))
        {
            sb.Append("<!--kg-card-begin: html-->").Append(html).Append("<!--kg-card-end: html-->");
        }
    }

    private static void RenderImageCard(Card card, StringBuilder sb)
    {
        var src = GetPayloadString(card.Payload, "src");
        if (string.IsNullOrEmpty(src)) return;

        var alt = GetPayloadString(card.Payload, "alt");
        var caption = GetPayloadString(card.Payload, "caption");
        var cardWidth = GetPayloadString(card.Payload, "cardWidth");

        var widthClass = cardWidth switch
        {
            "wide" => " kg-width-wide",
            "full" => " kg-width-full",
            _ => string.Empty,
        };

        sb.Append("<figure class=\"kg-card kg-image-card").Append(widthClass);
        if (!string.IsNullOrEmpty(caption))
            sb.Append(" kg-card-hascaption");
        sb.Append("\">");

        sb.Append("<img src=\"").Append(Encode(src)).Append('"');
        sb.Append(" class=\"kg-image\"");
        sb.Append(" alt=\"").Append(Encode(alt ?? string.Empty)).Append('"');
        sb.Append(" loading=\"lazy\" />");

        if (!string.IsNullOrEmpty(caption))
        {
            sb.Append("<figcaption>").Append(caption).Append("</figcaption>");
        }

        sb.Append("</figure>");
    }

    private static void RenderMarkdownCard(Card card, StringBuilder sb)
    {
        // Markdown cards store rendered HTML in the payload for Ghost 4.0+
        var markdown = GetPayloadString(card.Payload, "markdown");
        if (!string.IsNullOrEmpty(markdown))
        {
            sb.Append(Encode(markdown));
        }
    }

    private static void RenderCodeCard(Card card, StringBuilder sb)
    {
        var code = GetPayloadString(card.Payload, "code");
        if (string.IsNullOrEmpty(code)) return;

        var language = GetPayloadString(card.Payload, "language");
        var caption = GetPayloadString(card.Payload, "caption");

        sb.Append("<figure class=\"kg-card kg-code-card\">");
        sb.Append("<pre><code");
        if (!string.IsNullOrEmpty(language))
            sb.Append(" class=\"language-").Append(Encode(language)).Append('"');
        sb.Append('>');
        sb.Append(Encode(code));
        sb.Append("</code></pre>");

        if (!string.IsNullOrEmpty(caption))
            sb.Append("<figcaption>").Append(caption).Append("</figcaption>");

        sb.Append("</figure>");
    }

    private static void RenderEmbedCard(Card card, StringBuilder sb)
    {
        var html = GetPayloadString(card.Payload, "html");
        if (!string.IsNullOrEmpty(html))
        {
            sb.Append("<figure class=\"kg-card kg-embed-card\">")
              .Append(html)
              .Append("</figure>");
        }
    }

    private static void RenderBookmarkCard(Card card, StringBuilder sb)
    {
        var url = GetPayloadString(card.Payload, "url");
        if (string.IsNullOrEmpty(url)) return;

        var title = GetPayloadString(card.Payload, "title");
        var description = GetPayloadString(card.Payload, "description");
        var icon = GetPayloadString(card.Payload, "icon");
        var thumbnail = GetPayloadString(card.Payload, "thumbnail");
        var publisher = GetPayloadString(card.Payload, "publisher");

        sb.Append("<figure class=\"kg-card kg-bookmark-card\">");
        sb.Append("<a class=\"kg-bookmark-container\" href=\"").Append(Encode(url)).Append("\">");
        sb.Append("<div class=\"kg-bookmark-content\">");
        sb.Append("<div class=\"kg-bookmark-title\">").Append(Encode(title ?? string.Empty)).Append("</div>");
        sb.Append("<div class=\"kg-bookmark-description\">").Append(Encode(description ?? string.Empty)).Append("</div>");
        sb.Append("<div class=\"kg-bookmark-metadata\">");

        if (!string.IsNullOrEmpty(icon))
            sb.Append("<img class=\"kg-bookmark-icon\" src=\"").Append(Encode(icon)).Append("\" alt=\"\" />");

        sb.Append("<span class=\"kg-bookmark-publisher\">").Append(Encode(publisher ?? string.Empty)).Append("</span>");
        sb.Append("</div></div>");

        if (!string.IsNullOrEmpty(thumbnail))
        {
            sb.Append("<div class=\"kg-bookmark-thumbnail\">");
            sb.Append("<img src=\"").Append(Encode(thumbnail)).Append("\" alt=\"\" />");
            sb.Append("</div>");
        }

        sb.Append("</a></figure>");
    }

    private static void RenderGalleryCard(Card card, StringBuilder sb)
    {
        if (card.Payload.ValueKind == JsonValueKind.Undefined) return;
        if (!card.Payload.TryGetProperty("images", out var images)) return;

        sb.Append("<figure class=\"kg-card kg-gallery-card kg-width-wide\">");
        sb.Append("<div class=\"kg-gallery-container\">");

        foreach (var img in images.EnumerateArray())
        {
            var src = img.TryGetProperty("src", out var srcProp) ? srcProp.GetString() : null;
            if (string.IsNullOrEmpty(src)) continue;

            var alt = img.TryGetProperty("alt", out var altProp) ? altProp.GetString() : string.Empty;

            sb.Append("<div class=\"kg-gallery-image\">");
            sb.Append("<img src=\"").Append(Encode(src)).Append('"');
            sb.Append(" alt=\"").Append(Encode(alt ?? string.Empty)).Append('"');
            sb.Append(" loading=\"lazy\" />");
            sb.Append("</div>");
        }

        sb.Append("</div>");

        var caption = GetPayloadString(card.Payload, "caption");
        if (!string.IsNullOrEmpty(caption))
            sb.Append("<figcaption>").Append(caption).Append("</figcaption>");

        sb.Append("</figure>");
    }

    private static string? GetPayloadString(JsonElement payload, string property)
    {
        if (payload.ValueKind == JsonValueKind.Undefined)
            return null;
        return payload.TryGetProperty(property, out var prop) ? prop.GetString() : null;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private sealed record Markup(string TagName, List<KeyValuePair<string, string>> Attributes);
    private sealed record Atom(string Name, string Value, JsonElement Payload);
    private sealed record Card(string Name, JsonElement Payload);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
