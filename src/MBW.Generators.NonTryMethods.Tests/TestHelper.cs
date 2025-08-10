using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MBW.Generators.NonTryMethods;
using MBW.Generators.NonTryMethods.Abstracts.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MBW.Generators.NonTryMethods.Tests
{
    internal static class TestHelper
    {
        public static IReadOnlyDictionary<string, string> Run(string source)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GenerateNonTryMethodAttribute).Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
                "Tests",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            IIncrementalGenerator generator = new AutogenNonTryGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGenerators(compilation);
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            return runResult.Results.Single().GeneratedSources
                .ToDictionary(x => x.HintName, x => x.SourceText.ToString());
        }
    }
}
