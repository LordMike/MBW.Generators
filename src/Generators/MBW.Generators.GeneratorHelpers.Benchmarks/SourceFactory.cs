namespace MBW.Generators.GeneratorHelpers.Benchmarks;

static class SourceFactory
{
    // Build your exact namespace/type using raw strings + interpolation (C# 11)
    public static string MakeYourExample()
    {
        const string ns = "MBW.Generators.OverloadGenerator.Attributes";
        const string typeName = "TransformOverloadAttribute";

        // NOTE: raw interpolated string; braces in code are doubled {{ }} to escape from interpolation
        return $$"""
                 namespace {{ns}}
                 {
                     // Simple, empty marker type we’ll bind to in the benchmark
                     public class {{typeName}}
                     {
                     }
                 }
                 """;
    }
}