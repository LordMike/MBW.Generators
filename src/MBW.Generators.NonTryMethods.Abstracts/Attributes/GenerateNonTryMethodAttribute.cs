using System;
using System.Diagnostics;

namespace MBW.Generators.NonTryMethods.Abstracts.Attributes;

[Conditional("NEVER_RENDER")]
[AttributeUsage(AttributeTargets.Class)]
public class GenerateNonTryMethodAttribute : Attribute
{
}