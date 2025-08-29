using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MBW.Generators.NonTryMethods.Helpers;

internal static class AttributeValidation
{
    public static bool IsValidRegexPattern(string pattern, [NotNullWhen(true)]out Regex? regex)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            regex = null;
            return false;
        }

        try
        {
            regex = new Regex(pattern);

            // Require exactly one group
            return regex.GetGroupNumbers().Length == 2;
        }
        catch (ArgumentException)
        {
            // Invalid regex syntax
            regex = null;
            return false;
        }
    }
}