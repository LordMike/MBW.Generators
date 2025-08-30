using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MBW.Generators.OverloadGenerator.Helpers;

internal static class AttributeValidation
{
    public static bool IsValidRegexPattern(string pattern, [NotNullWhen(true)] out Regex? regex)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            regex = null;
            return false;
        }

        try
        {
            regex = new Regex(pattern);
            return true;
        }
        catch (ArgumentException)
        {
            regex = null;
            return false;
        }
    }
}
