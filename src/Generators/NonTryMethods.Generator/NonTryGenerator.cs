using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using MBW.Generators.Common;
using MBW.Generators.Common.Helpers;
using MBW.Generators.NonTryMethods.Generator.Helpers;
using MBW.Generators.NonTryMethods.Generator.GenerationModels;
using MBW.Generators.NonTryMethods.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MBW.Generators.NonTryMethods.Generator;

[Generator]
public sealed class NonTryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GeneratorCommon.Initialize<NonTryGenerator>(ref context, InitializeInternal);
    }

    private void InitializeInternal(ref IncrementalGeneratorInitializationContext context)
    {
        // Discover assembly-level attributes, once
        IncrementalValueProvider<ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern>>
            assemblyRuleProvider = context.CompilationProvider
                .Select((comp, _) => AttributesCollection.From(comp, comp.Assembly));

        // Find all classes+interfaces
        IncrementalValuesProvider<INamedTypeSymbol> allTypesProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax asType &&
                                    asType.Kind() is SyntaxKind.ClassDeclaration or
                                        SyntaxKind.InterfaceDeclaration or
                                        SyntaxKind.StructDeclaration or
                                        SyntaxKind.RecordDeclaration or
                                        SyntaxKind.RecordStructDeclaration,
                static (ctx, _) => (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node)!);

        // Filter all types, to those with assembly-level or type-level attributes
        IncrementalValuesProvider<TypeSpec> includedTypesProvider = allTypesProvider
            .Combine(context.CompilationProvider)
            .Combine(assemblyRuleProvider)
            .Where(static tuple =>
            {
                ((INamedTypeSymbol typeSymbol, Compilation _),
                    ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> assemblyAttributes) = tuple;

                Logger.Log($"Considering: {typeSymbol.Name}");

                if (assemblyAttributes.Length > 0)
                {
                    Logger.Log($"  Including because assembly attrib: {typeSymbol.Name}");
                    return true;
                }

                if (typeSymbol.GetAttributes().Any(a =>
                        a.AttributeClass.IsNamedExactlyTypeGenerateNonTryMethodAttribute()))
                {
                    Logger.Log($"  Including because type attrib: {typeSymbol.Name}");
                    return true;
                }

                return false;
            })
            .SelectMany(static (tuple, _) =>
            {
                ((INamedTypeSymbol typeSymbol, Compilation compilation),
                    ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> assemblyAttributes) = tuple;

                ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> classAttributes =
                    AttributesCollection.From(compilation, typeSymbol);

                Logger.Log($"  Determining methods for {typeSymbol.Name}");
                List<MethodSpec>? res = null;
                foreach (IMethodSymbol? method in typeSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    if (method.Name.Length == 0)
                        continue;

                    if (classAttributes.Any(a => a.Pattern.IsMatch(method.Name)))
                    {
                        res ??= [];
                        res.Add(new MethodSpec(method, classAttributes));
                        continue;
                    }

                    if (assemblyAttributes.Any(a => a.Pattern.IsMatch(method.Name)))
                    {
                        res ??= [];
                        res.Add(new MethodSpec(method, assemblyAttributes));
                    }
                }

                var options = NonTryCodeGen.GetEffectiveOptions(typeSymbol);

                if (res == null)
                    return ImmutableArray<TypeSpec>.Empty;

                TypeSpec typeSpec = new TypeSpec(typeSymbol, [..res], options);
                Logger.Log($"Type {typeSymbol.Name}, spec: {res.Count} methods, key: {typeSpec.Key}");
                return [typeSpec];
            });

        // Generate source
        context.RegisterSourceOutput(includedTypesProvider,
            (productionContext, typeSpec) =>
            {
                List<Diagnostic>? diagnostics = null;

                try
                {
                    TypeEmissionPlan plan = NonTryCodeGen.DetermineTypeStrategy(typeSpec);
                    ImmutableArray<PlannedMethod>
                        planned = NonTryCodeGen.PlanAllMethods(ref diagnostics, typeSpec, plan);
                    ImmutableArray<PlannedMethod> filtered =
                        NonTryCodeGen.FilterCollisionsAndDuplicates(ref diagnostics, typeSpec, planned);

                    Logger.Log(
                        $"Generating for {typeSpec.Type.Name}, plan: {plan}, methods: [{string.Join(", ", filtered.Select(x => x.Source.Method.Name))}]. Diagnostics: {diagnostics?.Count ?? 0}");

                    if (filtered.Length == 0)
                    {
                        Logger.Log(
                            $"Not emitting for {typeSpec.Type.Name}, no methods after filtering. Originally had {planned.Length} methods to generate for");
                        return;
                    }

                    CompilationUnitSyntax cu = NonTryCodeGen.BuildCompilationUnit(typeSpec, filtered,
                        needsTasks: filtered.Any(pm => pm.IsAsync));

                    string hintName = GenerationHelpers.GetHintName("NonTry", typeSpec.Type);

                    Logger.Log($"Emitting {hintName}");
                    productionContext.AddSource(hintName,
                        SourceText.From(cu.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
                }
                catch (Exception e)
                {
                    Logger.Log(e, "Generate threw an exception");
                }
                finally
                {
                    if (diagnostics != null)
                    {
                        Logger.Log($"Emitting {diagnostics.Count} diagnostics");
                        foreach (var sourceDiagnostic in diagnostics)
                            productionContext.ReportDiagnostic(sourceDiagnostic);
                    }
                }
            });
    }
}