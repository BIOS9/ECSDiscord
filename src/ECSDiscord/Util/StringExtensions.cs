using System;

namespace ECSDiscord.Util;

public static class StringExtensions
{
    public static string Truncate(this string input, int maxLength, bool addEllipsis = false)
    {
        if (maxLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength));
        }
        
        if (input.Length <= maxLength)
        {
            return input;
        }

        return input[..maxLength] + (addEllipsis ? "..." : string.Empty);
    }
}