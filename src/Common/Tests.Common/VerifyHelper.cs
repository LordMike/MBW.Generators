// VerifyConfig.cs

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

public static class VerifyHelper
{
    /// <summary>
    /// Windows invalid path chars (the most restrictive of the lot)
    /// </summary>
    private static char[] InvalidFileNameChars =>
    [
        '\"', '<', '>', '|', '\0',
        (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
        (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
        (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
        (char)31, ':', '*', '?', '\\', '/'
    ];
    
    public static Task VerifyObjectAsync(
        object target,
        string? name = null,
        bool replaceBaseName = false,
        [CallerMemberName] string? testMethod = null)
    {
        var settings = new VerifySettings();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var safe = MakeFileNameSafe(name!);

            // UseFileName replaces the base; compute what we want first.
            var baseName = replaceBaseName
                ? safe
                : $"{testMethod}__{safe}";

            settings.UseFileName(baseName);
        }

        return Verifier.Verify(target, settings);
    }

    public static Task VerifyGeneratorAsync(
        string? output,
        IReadOnlyList<Diagnostic> diagnostics,
        string? name = null,
        bool replaceBaseName = false,
        [CallerMemberName] string? testMethod = null)
    {
        var simplifiedDiagnostics = diagnostics.Select(s => new
        {
            Description = s.ToString()
        }).ToArray();

        return VerifyObjectAsync(new
        {
            output,
            diagnostics = simplifiedDiagnostics
        }, name, replaceBaseName, testMethod);
    }

    private static string MakeFileNameSafe(string value)
    {
        foreach (var c in InvalidFileNameChars)
            value = value.Replace(c, '_');
        return value.Trim();
    }
}