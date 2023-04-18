using System;
using System.Linq;

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
    
    private static Random _random = new Random();

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}