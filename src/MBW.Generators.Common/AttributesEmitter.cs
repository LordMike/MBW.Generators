using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MBW.Generators.Common;

internal static class AttributesEmitter
{
    private static Regex NameRegex = new Regex(@"ToEmit/(?<name>.*?)\.cs", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

    public static void EmitAttributes(Assembly assembly, ref IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            var assemblyName = assembly.GetName().Name;
            foreach (var name in assembly.GetManifestResourceNames())
            {
                var mtch = NameRegex.Match(name);
                if (!mtch.Success)
                    continue;

                var fileName = $"{assemblyName}.{mtch.Groups["name"].Value}.g.cs";

                using var strm = assembly.GetManifestResourceStream(name);
                ctx.AddSource(fileName, SourceText.From(strm));
            }
        });
    }
}