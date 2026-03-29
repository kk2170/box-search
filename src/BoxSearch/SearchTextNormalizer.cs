using System;
using System.Text;

namespace BoxSearch;

internal static class SearchTextNormalizer
{
    internal static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(value.Length);

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character) || char.IsWhiteSpace(character))
            {
                buffer.Append(char.ToLowerInvariant(character));
            }
        }

        return CollapseWhitespace(buffer.ToString());
    }

    private static string CollapseWhitespace(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        var buffer = new StringBuilder(value.Length);
        var pendingWhitespace = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingWhitespace = buffer.Length > 0;
                continue;
            }

            if (pendingWhitespace)
            {
                buffer.Append(' ');
                pendingWhitespace = false;
            }

            buffer.Append(character);
        }

        return buffer.ToString();
    }
}
