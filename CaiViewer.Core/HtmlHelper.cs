using System.Net;
using System.Text.RegularExpressions;

namespace CaiViewer.Core;

/// <summary>
/// Converts HTML-encoded text (as returned by CivitAI descriptions) to readable plain text.
/// Preserves line breaks from &lt;br&gt;, &lt;p&gt;, &lt;li&gt; etc.
/// </summary>
public static partial class HtmlHelper
{
    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrTag();

    [GeneratedRegex(@"</?(p|div|li|h[1-6]|tr|blockquote)[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockTag();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultipleNewlines();

    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var text = html;
        // Convert <br> to newline
        text = BrTag().Replace(text, "\n");
        // Convert block-level tags to newline
        text = BlockTag().Replace(text, "\n");
        // Remove remaining tags
        text = AnyTag().Replace(text, string.Empty);
        // Decode HTML entities (&amp; &lt; &gt; &#160; etc.)
        text = WebUtility.HtmlDecode(text);
        // Collapse excessive blank lines
        text = MultipleNewlines().Replace(text, "\n\n");

        return text.Trim();
    }
}
