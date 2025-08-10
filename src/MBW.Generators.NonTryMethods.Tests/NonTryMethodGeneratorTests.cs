using System.Collections.Generic;
using MBW.Generators.NonTryMethods.Tests.Helpers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace MBW.Generators.NonTryMethods.Tests;

public class NonTryMethodGeneratorTests
{
    [Fact]
    public void GeneratesForPublicTryMethod()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper
            .RunHelper("""
                       using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                       namespace Test
                       {
                           [GenerateNonTryMethod]
                           public static class Sample
                           {
                               public static bool TryParsePublic(this string value, out int result)
                               {
                                   result = 0;
                                   return true;
                               }
                           }
                       }

                       """);

        Assert.Empty(diags);
        TestsHelper.CheckEqual("""
                               using System;
                               using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                               namespace Test
                               {
                                   public static class Sample_AutogenNonTry
                                   {
                                       public static int ParsePublic(this string value)
                                       {
                                           if (!value.TryParsePublic(out int result))
                                               throw new Exception();

                                           return result;
                                       }
                                   }
                               }
                               """, output);
    }

    [Fact]
    public void GeneratesForInternalTryMethod()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper
            .RunHelper("""
                       using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                       namespace Test
                       {
                           [GenerateNonTryMethod]
                           public static class Sample
                           {
                               internal static bool TryParseInternal(this string value, out int result)
                               {
                                   result = 0;
                                   return true;
                               }
                           }
                       }
                       """);

        Assert.Empty(diags);
        TestsHelper.CheckEqual("""
                               using System;
                               using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                               namespace Test
                               {
                                   public static class Sample_AutogenNonTry
                                   {
                                       internal static int ParseInternal(this string value)
                                       {
                                           if (!value.TryParseInternal(out int result))
                                               throw new Exception();

                                           return result;
                                       }
                                   }
                               }
                               """, output);
    }

    [Fact]
    public void GeneratesForPrivateTryMethod()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper
            .RunHelper("""
                       using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                       namespace Test
                       {
                           [GenerateNonTryMethod]
                           public static class Sample
                           {
                               private static bool TryParsePrivate(this string value, out int result)
                               {
                                   result = 0;
                                   return true;
                               }
                           }
                       }

                       """);

        Assert.Empty(diags);
        TestsHelper.CheckEqual("""
                               using System;
                               using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                               namespace Test
                               {
                                   public static class Sample_AutogenNonTry
                                   {
                                       private static int ParsePrivate(this string value)
                                       {
                                           if (!value.TryParsePrivate(out int result))
                                               throw new Exception();

                                           return result;
                                       }
                                   }
                               }

                               """, output);
    }

    [Fact]
    public void GeneratesForRefArgument()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper
            .RunHelper("""
                       using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                       namespace Test
                       {
                           [GenerateNonTryMethod]
                           public static class Sample
                           {
                               public static bool TryWithRef(this string value, ref int position, out int result)
                               {
                                   result = position;
                                   return true;
                               }
                           }
                       }

                       """);
        Assert.Empty(diags);
        TestsHelper.CheckEqual("""
                               using System;
                               using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                               namespace Test
                               {
                                   public static class Sample_AutogenNonTry
                                   {
                                       public static int WithRef(this string value, ref int position)
                                       {
                                           if (!value.TryWithRef(position, out int result))
                                               throw new Exception();

                                           return result;
                                       }
                                   }
                               }
                               """, output);
    }

    [Fact]
    public void GeneratesForOutArgument()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper
            .RunHelper("""
                       using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                       namespace Test
                       {
                           [GenerateNonTryMethod]
                           public static class Sample
                           {
                               public static bool TryWithOut(this string value, out int result)
                               {
                                   result = 1;
                                   return true;
                               }
                           }
                       }
                       """);

        Assert.Empty(diags);
        TestsHelper.CheckEqual("""
                               using System;
                               using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                               namespace Test
                               {
                                   public static class Sample_AutogenNonTry
                                   {
                                       public static int WithOut(this string value)
                                       {
                                           if (!value.TryWithOut(out int result))
                                               throw new Exception();

                                           return result;
                                       }
                                   }
                               }

                               """, output);
    }

    [Fact]
    public void GeneratesForNoOutArgument()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper
            .RunHelper("""
                       using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                       namespace Test
                       {
                           [GenerateNonTryMethod]
                           public static class Sample
                           {
                               public static bool TryNoOut(this string value)
                               {
                                   return true;
                               }
                           }
                       }

                       """);
        Assert.Empty(diags);
        TestsHelper.CheckEqual("""
                               using System;
                               using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                               namespace Test
                               {
                                   public static class Sample_AutogenNonTry
                                   {
                                       public static void NoOut(this string value)
                                       {
                                           if (!value.TryNoOut(out void result))
                                               throw new Exception();

                                           return result;
                                       }
                                   }
                               }

                               """, output);
    }

    [Fact]
    public void GeneratesForTwoOutArguments()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper
            .RunHelper("""
                       using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                       namespace Test
                       {
                           [GenerateNonTryMethod]
                           public static class Sample
                           {
                               public static bool TryTwoOut(this string value, out int first, out int second)
                               {
                                   first = 0; second = 0;
                                   return true;
                               }
                           }
                       }

                       """);
        
        Assert.Empty(diags);
        TestsHelper.CheckEqual("""
                               using System;
                               using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                               namespace Test
                               {
                                   public static class Sample_AutogenNonTry
                                   {
                                       public static void TwoOut(this string value, out int first, out int second)
                                       {
                                           if (!value.TryTwoOut(first, second, out void result))
                                               throw new Exception();

                                           return result;
                                       }
                                   }
                               }

                               """, output);
    }

    [Fact]
    public void GeneratesForTaskBool()
    {
        (string? output, IReadOnlyList<Diagnostic> diags) = TestsHelper
            .RunHelper("""
                                                                                  using MBW.Generators.NonTryMethods.Abstracts.Attributes;
                                                                                  using System.Threading.Tasks;
                                                                                  namespace Test
                                                                                  {
                                                                                      [GenerateNonTryMethod]
                                                                                      public static class Sample
                                                                                      {
                                                                                          public static Task<bool> TryAsync(this string value, out int result)
                                                                                          {
                                                                                              result = 0;
                                                                                              return Task.FromResult(true);
                                                                                          }
                                                                                      }
                                                                                  }

                                                                                  """);
        
        Assert.Empty(diags);
        Assert.Null(output);
    }
}