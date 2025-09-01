using System.Runtime.CompilerServices;
using DiffEngine;

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
            var assemblyName = type.Assembly.GetName().Name;
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