using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Tests;

public class Functionality
{
    [Fact]
    public async Task SyncBoolOut_Defaults()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public bool TryLoad(int a, int b, out int value)
                                      {
                                          value = a + b;
                                          return a >= 0 && b >= 0;
                                      }
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task SyncBoolOut_CustomException()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  using System;
                                  
                                  public class MyEx : Exception {}

                                  [GenerateNonTryMethod(typeof(MyEx))]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public bool TryLoad(int a, int b, out int value)
                                      {
                                          value = a + b;
                                          return false;
                                      }
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task TaskTuple_Verbatim()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  using System.Threading.Tasks;
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public Task<(bool ok, string? value)> TryLoadAsync(int id)
                                          => Task.FromResult((id > 0, id > 0 ? $"#{id}" : null));
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task ValueTaskTuple_Verbatim()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  using System.Threading.Tasks;
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions]
                                  public partial class TestClass
                                  {
                                      public ValueTask<(bool ok, int? value)> TryLoadAsync(int id)
                                          => new ValueTask<(bool,int?)>((id > 0, id > 0 ? id : (int?)null));
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }

    [Fact]
    public async Task TaskTuple_TrueMeansNotNull_ReferencePayload()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) =
            await TestsHelper.RunHelperAsync("""
                                  using System.Threading.Tasks;
                                  
                                  [GenerateNonTryMethod]
                                  [GenerateNonTryOptions(returnGenerationStrategy: ReturnGenerationStrategy.TrueMeansNotNull)]
                                  public partial class TestClass
                                  {
                                      public Task<(bool ok, string? value)> TryLoadAsync(int id)
                                          => Task.FromResult((id > 0, id > 0 ? $"#{id}" : null));
                                  }
                                  """);
        await VerifyHelper.VerifyGeneratorAsync(output, diags);
    }
}