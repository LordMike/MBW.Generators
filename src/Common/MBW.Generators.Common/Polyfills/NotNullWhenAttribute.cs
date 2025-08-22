// ReSharper disable CheckNamespace
#pragma warning disable CS9113 // Parameter is unread.

namespace System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Polyfill")]
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute;