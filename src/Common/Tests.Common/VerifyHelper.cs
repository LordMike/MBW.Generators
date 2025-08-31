// VerifyConfig.cs

using System.Runtime.CompilerServices;
using DiffEngine;
using Microsoft.CodeAnalysis;

internal static class VerifyConfig
{
    [ModuleInitializer]
    public static void Init()
    {
        // Never launch WinMerge/any diff tool
        DiffRunner.Disabled = true;

        // Directory: <project>/VerifyFiles/<namespace> ; base = test method name
        DerivePathInfo((sourceFile, projectDir, type, method) =>
        {
            var assemblyName =  type.Assembly.GetName().Name;
            var typeNamespace = type.Namespace;

            if (!typeNamespace.StartsWith(assemblyName, StringComparison.Ordinal))
                throw new InvalidOperationException("The test run was not in the " + assemblyName + " namespace");
            
            typeNamespace = typeNamespace.Substring(assemblyName.Length)
                .TrimStart('.');
            
            var nsPath = typeNamespace.Replace('.', Path.DirectorySeparatorChar);
            var directory = Path.Combine(projectDir, "VerifyFiles", nsPath, type.Name);

            return new PathInfo(
                directory: directory,
                typeName: type.Name,
                methodName: method.Name);
        });
    }
}

public static class VerifyHelper
{
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
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return value.Trim();
    }
}