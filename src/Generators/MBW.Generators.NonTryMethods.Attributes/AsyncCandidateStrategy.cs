using System;
using System.Diagnostics.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Attributes;

[Flags]
[SuppressMessage("ReSharper", "RedundantNameQualifier")]
public enum AsyncCandidateStrategy
{
    /// <summary>
    /// Do not generate non-try methods for async Try methods
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Include try methods that return a Task&gt;(bool, T)&lt;. It is assumed that the boolean (first parameter) indicates success while the T (second parameter) is the value to return. 
    /// </summary>
    TupleBooleanAndValue = 1
}