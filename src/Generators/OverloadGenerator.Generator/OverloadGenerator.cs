using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using MBW.Generators.Common;
using MBW.Generators.Common.Helpers;
using MBW.Generators.OverloadGenerator.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.OverloadGenerator.Generator;

[Generator]
public sealed class OverloadGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GeneratorCommon.Initialize<OverloadGenerator>(ref context, InitializeInternal);
    }

    private void InitializeInternal(ref IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<KnownSymbols?> knownSymbolsProvider =
            context.CompilationProvider.Select((comp, _) => KnownSymbols.TryCreateInstance(comp));

        IncrementalValueProvider<AttributesCollection> assemblyAttributesProvider = knownSymbolsProvider
            .Combine(context.CompilationProvider)
            .Select((t, _) => AttributesCollection.From(t.Left, t.Right.Assembly));

        IncrementalValuesProvider<INamedTypeSymbol> allTypesProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax asType &&
                                    asType.Kind() is SyntaxKind.ClassDeclaration or
                                        SyntaxKind.InterfaceDeclaration or
                                        SyntaxKind.StructDeclaration or
                                        SyntaxKind.RecordDeclaration or
                                        SyntaxKind.RecordStructDeclaration,
                static (ctx, _) => (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node)!);

        IncrementalValuesProvider<TypeSpec> includedTypesProvider = allTypesProvider
            .Combine(knownSymbolsProvider)
            .Combine(assemblyAttributesProvider)
            .SelectMany(static (tuple, _) =>
            {
                ((INamedTypeSymbol? typeSymbol, KnownSymbols? knownSymbols), AttributesCollection assemblyAttributes) =
                    tuple;

                if (knownSymbols == null)
                    return ImmutableArray<TypeSpec>.Empty;

                AttributesCollection classAttributes = AttributesCollection.From(knownSymbols, typeSymbol);

                bool hasAnyAttributes =
                    !assemblyAttributes.DefaultAttributes.IsDefaultOrEmpty ||
                    !assemblyAttributes.TransformAttributes.IsDefaultOrEmpty ||
                    !classAttributes.DefaultAttributes.IsDefaultOrEmpty ||
                    !classAttributes.TransformAttributes.IsDefaultOrEmpty;

                if (!hasAnyAttributes)
                    return ImmutableArray<TypeSpec>.Empty;

                List<MethodSpec>? methods = null;
                foreach (IMethodSymbol method in typeSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    foreach (var attr in assemblyAttributes.DefaultAttributes)
                    {
                        var param = TryFindMatchingParameter(method, attr.MethodNamePattern, attr.ParameterNamePattern,
                            attr.ParameterType);
                        if (param != null)
                        {
                            methods ??= new();
                            methods.Add(new MethodSpec(method, new DefaultRule(param, attr.DefaultExpression)));
                        }
                    }

                    foreach (var attr in classAttributes.DefaultAttributes)
                    {
                        var param = TryFindMatchingParameter(method, attr.MethodNamePattern, attr.ParameterNamePattern,
                            attr.ParameterType);
                        if (param != null)
                        {
                            methods ??= new();
                            methods.Add(new MethodSpec(method, new DefaultRule(param, attr.DefaultExpression)));
                        }
                    }

                    foreach (var attr in assemblyAttributes.TransformAttributes)
                    {
                        var param = TryFindMatchingParameter(method, attr.MethodNamePattern, attr.ParameterNamePattern,
                            attr.ParameterType);
                        if (param != null)
                        {
                            methods ??= new();
                            methods.Add(new MethodSpec(method,
                                new TransformRule(param, attr.NewType, attr.TransformExpression,
                                    attr.NewTypeNullability)));
                        }
                    }

                    foreach (var attr in classAttributes.TransformAttributes)
                    {
                        var param = TryFindMatchingParameter(method, attr.MethodNamePattern, attr.ParameterNamePattern,
                            attr.ParameterType);
                        if (param != null)
                        {
                            methods ??= new();
                            methods.Add(new MethodSpec(method,
                                new TransformRule(param, attr.NewType, attr.TransformExpression,
                                    attr.NewTypeNullability)));
                        }
                    }
                }

                if (methods == null)
                    return ImmutableArray<TypeSpec>.Empty;

                return [new TypeSpec(typeSymbol, [..methods])];
            });

        context.RegisterSourceOutput(includedTypesProvider, static (spc, typeSpec) =>
        {
            List<Diagnostic>? diagnostics = null;
            string? file = OverloadCodeGen.Generate(typeSpec, ref diagnostics);
            if (file is not null)
            {
                string hint = GenerationHelpers.GetHintName("Overloads", typeSpec.Type);
                spc.AddSource(hint, file);
            }

            if (diagnostics != null)
                foreach (var d in diagnostics)
                    spc.ReportDiagnostic(d);
        });
    }

    static string? TryFindMatchingParameter(IMethodSymbol method,
        Regex attrMethodNamePattern,
        Regex attrParameterNamePattern, INamedTypeSymbol? attrParameterType)
    {
        if (!attrMethodNamePattern.IsMatch(method.Name))
            return null;

        foreach (var p in method.Parameters)
        {
            if (!attrParameterNamePattern.IsMatch(p.Name))
                continue;
            if (attrParameterType != null &&
                !SymbolEqualityComparer.Default.Equals(p.Type, attrParameterType))
                continue;

            return p.Name;
        }

        return null;
    }
}