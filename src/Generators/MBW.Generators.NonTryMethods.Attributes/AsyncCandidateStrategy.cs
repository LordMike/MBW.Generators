using System;
using System.Diagnostics.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Attributes;

/// <summary>
/// Determines which asynchronous <c>Try</c> method shapes are considered for
/// generating non-<c>Try</c> counterparts. Values can be combined.
/// </summary>
[Flags]
[SuppressMessage("ReSharper", "RedundantNameQualifier")]
public enum AsyncCandidateStrategy
{
    /// <summary>
    /// Disable generation for asynchronous <c>Try</c> methods.
    /// </summary>
    None = 0,

    /// <summary>
    /// Include methods returning <c>Task&lt;(bool Success, T Value)&gt;</c> where the
    /// boolean indicates success and <c>T</c> is the value to return.
    /// </summary>
    TupleBooleanAndValue = 1,
}
