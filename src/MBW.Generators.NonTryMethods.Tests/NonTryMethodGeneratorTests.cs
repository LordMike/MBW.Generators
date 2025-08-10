using Xunit;

namespace MBW.Generators.NonTryMethods.Tests
{
    public class NonTryMethodGeneratorTests
    {
        [Fact]
        public void GeneratesForPublicTryMethod()
        {
            const string input = @"using MBW.Generators.NonTryMethods.Abstracts.Attributes;
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
";
            const string expected = @"using System;
using MBW.Generators.NonTryMethods.Abstracts.Attributes;
namespace Test
{
    public static class 
Sample_AutogenNonTry
    {
        public static int ParsePublic(this string value)
        {
            if (!value.TryParsePublic(out int result))
                throw new Exception();

            return result;
        }
    }
}
";
            var output = TestHelper.Run(input);
            Assert.Equal(expected, Assert.Single(output).Value);
        }

        [Fact]
        public void GeneratesForInternalTryMethod()
        {
            const string input = @"using MBW.Generators.NonTryMethods.Abstracts.Attributes;
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
";
            const string expected = @"using System;
using MBW.Generators.NonTryMethods.Abstracts.Attributes;
namespace Test
{
    public static class 
Sample_AutogenNonTry
    {
        internal static int ParseInternal(this string value)
        {
            if (!value.TryParseInternal(out int result))
                throw new Exception();

            return result;
        }
    }
}
";
            var output = TestHelper.Run(input);
            Assert.Equal(expected, Assert.Single(output).Value);
        }

        [Fact]
        public void GeneratesForPrivateTryMethod()
        {
            const string input = @"using MBW.Generators.NonTryMethods.Abstracts.Attributes;
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
";
            const string expected = @"using System;
using MBW.Generators.NonTryMethods.Abstracts.Attributes;
namespace Test
{
    public static class 
Sample_AutogenNonTry
    {
        private static int ParsePrivate(this string value)
        {
            if (!value.TryParsePrivate(out int result))
                throw new Exception();

            return result;
        }
    }
}
";
            var output = TestHelper.Run(input);
            Assert.Equal(expected, Assert.Single(output).Value);
        }

        [Fact]
        public void GeneratesForRefArgument()
        {
            const string input = @"using MBW.Generators.NonTryMethods.Abstracts.Attributes;
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
";
            const string expected = @"using System;
using MBW.Generators.NonTryMethods.Abstracts.Attributes;
namespace Test
{
    public static class 
Sample_AutogenNonTry
    {
        public static int WithRef(this string value, ref int position)
        {
            if (!value.TryWithRef(position, out int result))
                throw new Exception();

            return result;
        }
    }
}
";
            var output = TestHelper.Run(input);
            Assert.Equal(expected, Assert.Single(output).Value);
        }

        [Fact]
        public void GeneratesForOutArgument()
        {
            const string input = @"using MBW.Generators.NonTryMethods.Abstracts.Attributes;
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
";
            const string expected = @"using System;
using MBW.Generators.NonTryMethods.Abstracts.Attributes;
namespace Test
{
    public static class 
Sample_AutogenNonTry
    {
        public static int WithOut(this string value)
        {
            if (!value.TryWithOut(out int result))
                throw new Exception();

            return result;
        }
    }
}
";
            var output = TestHelper.Run(input);
            Assert.Equal(expected, Assert.Single(output).Value);
        }

        [Fact]
        public void GeneratesForNoOutArgument()
        {
            const string input = @"using MBW.Generators.NonTryMethods.Abstracts.Attributes;
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
";
            const string expected = @"using System;
using MBW.Generators.NonTryMethods.Abstracts.Attributes;
namespace Test
{
    public static class 
Sample_AutogenNonTry
    {
        public static void NoOut(this string value)
        {
            if (!value.TryNoOut(out void result))
                throw new Exception();

            return result;
        }
    }
}
";
            var output = TestHelper.Run(input);
            Assert.Equal(expected, Assert.Single(output).Value);
        }

        [Fact]
        public void GeneratesForTwoOutArguments()
        {
            const string input = @"using MBW.Generators.NonTryMethods.Abstracts.Attributes;
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
";
            const string expected = @"using System;
using MBW.Generators.NonTryMethods.Abstracts.Attributes;
namespace Test
{
    public static class 
Sample_AutogenNonTry
    {
        public static void TwoOut(this string value, out int first, out int second)
        {
            if (!value.TryTwoOut(first, second, out void result))
                throw new Exception();

            return result;
        }
    }
}
";
            var output = TestHelper.Run(input);
            Assert.Equal(expected, Assert.Single(output).Value);
        }

        [Fact]
        public void GeneratesForTaskBool()
        {
            const string input = @"using MBW.Generators.NonTryMethods.Abstracts.Attributes;
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
";
            var output = TestHelper.Run(input);
            Assert.Empty(output);
        }
    }
}
