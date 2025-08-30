using BenchmarkDotNet.Running;

namespace MBW.Generators.GeneratorHelpers.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // var p1 =  new SymbolCompareBenchmarks();
        // p1.Setup();
        //
        // Console.WriteLine(p1.Manual_MetadataName_And_Namespace_Equals());
        // Console.WriteLine(p1.Manual_MetadataName_And_Namespace_Equals_Unrolled());
        
        BenchmarkRunner.Run<SymbolCompareBenchmarks>();
    }
}