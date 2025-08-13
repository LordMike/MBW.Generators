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

    public static (GenerateNonTryOptionsAttributeInfo info, Location origin) ToAsyncOptions(in AttributeData a)
    {
        AsyncCandidateStrategy asyncCandidateStrategy = AsyncCandidateStrategy.TupleBooleanAndValue;
        ReturnGenerationStrategy returnGenerationStrategy = ReturnGenerationStrategy.Verbatim;
        MethodsGenerationStrategy methodsGenerationStrategy = MethodsGenerationStrategy.Auto;

        ImmutableArray<TypedConstant> args = a.ConstructorArguments;
        if (args.Length >= 1 && args[0].Value is int c) asyncCandidateStrategy = (AsyncCandidateStrategy)c;
        if (args.Length >= 2 && args[1].Value is int g) returnGenerationStrategy = (ReturnGenerationStrategy)g;
        if (args.Length >= 3 && args[2].Value is int m) methodsGenerationStrategy = (MethodsGenerationStrategy)m;

        return (
            new GenerateNonTryOptionsAttributeInfo(asyncCandidateStrategy, returnGenerationStrategy, methodsGenerationStrategy),
            a.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None
        );
    }
}