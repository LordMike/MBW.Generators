using System.Collections.Generic;
using MBW.Generators.OverloadGenerator.Attributes;
using MBW.Generators.OverloadGenerator.Generator.Models;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Generator.Helpers;

internal static class AttributeConverters
{
    public static DefaultOverloadAttributeInfo ToDefault(AttributeData a)
    {
        string parameterNameRegex = a.ConstructorArguments.Length > 0 && a.ConstructorArguments[0].Value is string s ? s : "^(.*)$";
        string defaultExpression = a.ConstructorArguments.Length > 1 && a.ConstructorArguments[1].Value is string d ? d : string.Empty;

        INamedTypeSymbol? parameterType = null;
        string methodNameRegex = "^(.*)$";
        string? methodNameReplace = null;

        foreach (KeyValuePair<string, TypedConstant> kv in a.NamedArguments)
        {
            switch (kv.Key)
            {
                case "ParameterType" when kv.Value.Value is INamedTypeSymbol pts:
                    parameterType = pts;
                    break;
                case "MethodNameRegex" when kv.Value.Value is string mr && mr.Length != 0:
                    methodNameRegex = mr;
                    break;
                case "MethodNameReplace" when kv.Value.Value is string repl && repl.Length != 0:
                    methodNameReplace = repl;
                    break;
            }
        }

        return new DefaultOverloadAttributeInfo(
            a.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None,
            parameterNameRegex,
            parameterType,
            defaultExpression,
            methodNameRegex,
            methodNameReplace);
    }

    public static TransformOverloadAttributeInfo ToTransform(AttributeData a)
    {
        string parameterNameRegex = a.ConstructorArguments.Length > 0 && a.ConstructorArguments[0].Value is string s ? s : "^(.*)$";
        INamedTypeSymbol newType = (INamedTypeSymbol)a.ConstructorArguments[1].Value!;
        string transformExpression = a.ConstructorArguments.Length > 2 && a.ConstructorArguments[2].Value is string t ? t : "{value}.ToString()";
        TypeNullability nullability = TypeNullability.NotNullable;
        if (a.ConstructorArguments.Length > 3 && a.ConstructorArguments[3].Value is int n)
            nullability = (TypeNullability)n;

        INamedTypeSymbol? parameterType = null;
        string methodNameRegex = "^(.*)$";
        string? methodNameReplace = null;

        foreach (KeyValuePair<string, TypedConstant> kv in a.NamedArguments)
        {
            switch (kv.Key)
            {
                case "ParameterType" when kv.Value.Value is INamedTypeSymbol pts:
                    parameterType = pts;
                    break;
                case "MethodNameRegex" when kv.Value.Value is string mr && mr.Length != 0:
                    methodNameRegex = mr;
                    break;
                case "MethodNameReplace" when kv.Value.Value is string repl && repl.Length != 0:
                    methodNameReplace = repl;
                    break;
            }
        }

        return new TransformOverloadAttributeInfo(
            a.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None,
            parameterNameRegex,
            parameterType,
            newType,
            transformExpression,
            nullability,
            methodNameRegex,
            methodNameReplace);
    }
}
