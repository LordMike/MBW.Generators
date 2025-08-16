using System;
using System.Text.RegularExpressions;

namespace MBW.Generators.NonTryMethods.Helpers;

internal static class AttributeValidation
{
    public static bool IsValidRegexPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            Regex regex = new Regex(pattern);

            // Require exactly one group
            return regex.GetGroupNumbers().Length == 2;
        }
        catch (ArgumentException)
        {
            // Invalid regex syntax
            return false;
        }
    }
}