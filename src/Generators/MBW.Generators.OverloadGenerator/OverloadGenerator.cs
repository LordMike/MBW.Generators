using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MBW.Generators.Common;
using MBW.Generators.Common.Helpers;
using MBW.Generators.OverloadGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.OverloadGenerator;

[Generator]
public sealed class OverloadGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GeneratorCommon.Initialize<OverloadGenerator>(ref context, InitializeInternal);
    }

    private void InitializeInternal(ref IncrementalGeneratorInitializationContext context)
    {
#if ENABLE_PIPE_LOGGING
        context.RegisterSourceOutput(context.CompilationProvider, (_, _) => { Logger.Log("## Compilation run"); });
#endif

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

                ImmutableArray<DefaultOverloadAttributeInfoWithRegex> defaultAttrs = classAttributes.DefaultAttributes;
                if (!assemblyAttributes.DefaultAttributes.IsDefaultOrEmpty)
                    defaultAttrs = defaultAttrs.AddRange(assemblyAttributes.DefaultAttributes);

                ImmutableArray<TransformOverloadAttributeInfoWithRegex> transformAttrs =
                    classAttributes.TransformAttributes;
                if (!assemblyAttributes.TransformAttributes.IsDefaultOrEmpty)
                    transformAttrs = transformAttrs.AddRange(assemblyAttributes.TransformAttributes);

                List<MethodSpec>? methods = null;
                foreach (IMethodSymbol method in typeSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    List<Rule>? rules = null;

                    foreach (var attr in defaultAttrs)
                    {
                        if (!attr.MethodNamePattern.IsMatch(method.Name))
                            continue;

                        foreach (var p in method.Parameters)
                        {
                            if (!attr.ParameterNamePattern.IsMatch(p.Name))
                                continue;
                            if (attr.ParameterType != null &&
                                !SymbolEqualityComparer.Default.Equals(p.Type, attr.ParameterType))
                                continue;

                            rules ??= new();
                            rules.Add(new DefaultRule(p.Name, attr.DefaultExpression));
                        }
                    }

                    foreach (var attr in transformAttrs)
                    {
                        if (!attr.MethodNamePattern.IsMatch(method.Name))
                            continue;

                        foreach (var p in method.Parameters)
                        {
                            if (!attr.ParameterNamePattern.IsMatch(p.Name))
                                continue;
                            if (attr.ParameterType != null &&
                                !SymbolEqualityComparer.Default.Equals(p.Type, attr.ParameterType))
                                continue;

                            rules ??= new();
                            rules.Add(new TransformRule(p.Name, attr.NewType, attr.TransformExpression,
                                attr.NewTypeNullability));
                        }
                    }

                    if (rules != null)
                    {
                        methods ??= new();
                        methods.Add(new MethodSpec(method, [..rules]));
                    }
                }

                if (methods == null)
                    return ImmutableArray<TypeSpec>.Empty;

                return [new TypeSpec(knownSymbols, typeSymbol, [..methods])];
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
}