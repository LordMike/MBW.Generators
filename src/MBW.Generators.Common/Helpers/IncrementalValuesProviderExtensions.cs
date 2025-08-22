using Microsoft.CodeAnalysis;

namespace MBW.Generators.Common.Helpers;

internal static class IncrementalValuesProviderExtensions
{
    public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(
        this IncrementalValuesProvider<TSource?> source) where TSource : class
    {
        return source.SelectMany<TSource?, TSource>((item, token) =>
        {
            if (item == null)
                return [];

            return [item];
        });
    }
}