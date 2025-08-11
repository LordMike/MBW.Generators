using System.Collections.Immutable;
using MBW.Generators.NonTryMethods.Attributes;
using MBW.Generators.NonTryMethods.Models;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.Helpers;

internal static class AttributeConverters
{
    public static (GenerateNonTryMethodAttributeInfo info, Location origin) ToNonTry(in AttributeData a)
    {
        ITypeSymbol? exceptionType = null;
        string? pattern = "^[Tt]ry(.*)";

        ImmutableArray<TypedConstant> args = a.ConstructorArguments;
        if (args.Length >= 1 && args[0].Kind == TypedConstantKind.Type && args[0].Value is ITypeSymbol et)
            exceptionType = et;
        if (args.Length >= 2 && args[1].Value is string p && p.Length != 0)
            pattern = p;

        return (
            info: new GenerateNonTryMethodAttributeInfo(exceptionType, pattern),
            origin: a.ApplicationSyntaxReference?.GetSyntax()?.GetLocation() ?? Location.None
        );
    }

    public static (GenerateNonTryAsyncOptionsAttributeInfo info, Location origin) ToAsyncOptions(in AttributeData a)
    {
        AsyncCandidateStrategy candidate = AsyncCandidateStrategy.TupleBooleanAndValue; // default
        AsyncGenerationStrategy generation = AsyncGenerationStrategy.Verbatim; // default

        ImmutableArray<TypedConstant> args = a.ConstructorArguments;
        if (args.Length >= 1 && args[0].Value is int c) candidate = (AsyncCandidateStrategy)c;
        if (args.Length >= 2 && args[1].Value is int g) generation = (AsyncGenerationStrategy)g;

        return (
            new GenerateNonTryAsyncOptionsAttributeInfo(candidate, generation),
            a.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None
        );
    }
}