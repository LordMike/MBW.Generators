using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using MBW.Generators.Common;
using MBW.Generators.Common.Helpers;
using MBW.Generators.NonTryMethods.GenerationModels;
using MBW.Generators.NonTryMethods.Helpers;
using MBW.Generators.NonTryMethods.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MBW.Generators.NonTryMethods;

[Generator]
public sealed class NonTryGenerator : GeneratorBase<NonTryGenerator>
{
    protected override void InitializeInternal(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (_, _) => { Logger.Log("## Compilation run"); });

        // Known types by reference, empty if not present
        IncrementalValueProvider<KnownSymbols?> knownSymbolsProvider =
            context.CompilationProvider.Select((comp, _) =>
            {
                KnownSymbols? tryCreateInstance = KnownSymbols.TryCreateInstance(comp);
                Logger.Log($"Symbols: {tryCreateInstance}");
                return tryCreateInstance;
            });

        // Discover assembly-level attributes, once
        IncrementalValueProvider<ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern>>
            assemblyRuleProvider = knownSymbolsProvider
                .Combine(context.CompilationProvider)
                .Select((t, _) => AttributesCollection.From(t.Left, t.Right.Assembly));

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
            .Combine(knownSymbolsProvider)
            .Combine(assemblyRuleProvider)
            .Where(static tuple =>
            {
                // Evaluate if the type should be considered for generation
                ((INamedTypeSymbol? typeSymbol, KnownSymbols? knownSymbols),
                    ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> assemblyAttributes) = tuple;

                if (knownSymbols == null)
                    return false;

                Logger.Log($"Considering: {typeSymbol.Name}");

                // If assembly has attribute => include
                if (assemblyAttributes.Length > 0)
                {
                    Logger.Log($"  Including because assembly attrib: {typeSymbol.Name}");
                    return true;
                }

                // If type has attribute => include
                if (typeSymbol.GetAttributes().Any(a =>
                        a.AttributeClass?.Equals(knownSymbols.GenerateNonTryMethodAttribute,
                            SymbolEqualityComparer.Default) ?? false))
                {
                    Logger.Log($"  Including because type attrib: {typeSymbol.Name}");
                    return true;
                }

                return false;
            })
            .SelectMany(static (tuple, _) =>
            {
                // Produce a type-spec, if any, for this type
                ((INamedTypeSymbol? typeSymbol, KnownSymbols? knownSymbols),
                    ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> assemblyAttributes) = tuple;
                ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> classAttributes =
                    AttributesCollection.From(knownSymbols, typeSymbol);

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

                if (res == null)
                    return ImmutableArray<TypeSpec>.Empty;

                TypeSpec typeSpec = new TypeSpec(knownSymbols!, typeSymbol, [..res]);
                Logger.Log($"Type {typeSymbol.Name}, spec: {res.Count} methods, key: {typeSpec.Key}");
                return [typeSpec];
            });

        // Generate source
        // Bonus: Also ensure we render on EACH iteration, due to CompilationProvider
        IncrementalValuesProvider<TypeSource> sourceProvider = includedTypesProvider
            .Select((typeSpec, _) =>
            {
                List<Diagnostic>? diagnostics = null;

                Logger.Log($"Generating for key: {typeSpec.Type.Name}");

                try
                {
                    TypeEmissionPlan plan = Gen.DetermineTypeStrategy(typeSpec);
                    ImmutableArray<PlannedMethod>
                        planned = Gen.PlanAllMethods(ref diagnostics, typeSpec, plan);
                    ImmutableArray<PlannedMethod> filtered =
                        Gen.FilterCollisionsAndDuplicates(ref diagnostics, typeSpec, planned);

                    Logger.Log($"Generating for {typeSpec.Type.Name}, plan: {plan}, methods: {string.Join(", ", filtered.Select(x => x.Source.Method.Name))}");

                    if (filtered.Length == 0)
                    {
                        Logger.Log("WARNING Not emitting");
                        return default;
                    }

                    CompilationUnitSyntax cu = Gen.BuildCompilationUnit(typeSpec, filtered,
                        needsTasks: filtered.Any(pm => pm.IsAsync));

                    return new TypeSource(GenerationHelpers.GetHintName("NonTry", typeSpec.Type),
                        SourceText.From(cu.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
                }
                catch (Exception e)
                {
                    Logger.Log(e, "Generate");
                    return default;
                }
            });

        context.RegisterSourceOutput(sourceProvider,
            (productionContext, source) =>
            {
                if (source == default)
                    return;

                try
                {
                    Logger.Log("Emitting " + source.HintName);
                    productionContext.AddSource(source.HintName, source.Source);
                }
                catch (Exception e)
                {
                    Logger.Log(e, "Emit");
                }
            });

        HandleErrorDiagnostics(context);
    }

    private static void HandleErrorDiagnostics(IncrementalGeneratorInitializationContext context)
    {
        // Prepare known symbols
        IncrementalValueProvider<INamedTypeSymbol?> symbolProvider = context.CompilationProvider
            .Select((compilation, _) => compilation.GetTypeByMetadataName("System.Exception"));

        // NonTry Attributes that are invalid
        IncrementalValuesProvider<AttributeData> attributesProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: KnownSymbols.NonTryAttribute,
                predicate: static (_, _) => true, // already filtered by name; keep cheap
                transform: static (ctx, _) => ctx.Attributes)
            .SelectMany((datas, _) => datas);

        IncrementalValuesProvider<Diagnostic> diagnostics =
            attributesProvider.Combine(symbolProvider)
                .SelectMany((tuple, token) =>
                {
                    (AttributeData? attributeData, INamedTypeSymbol? exceptionSymbol) = tuple;

                    Location loc = attributeData.ApplicationSyntaxReference?.GetSyntax(token).GetLocation() ?? Location.None;

                    GenerateNonTryMethodAttributeInfo info = AttributeConverters.ToNonTry(in attributeData);
                    string pattern = info.MethodNamePattern;

                    List<Diagnostic>? results = null;
                    if (string.IsNullOrWhiteSpace(pattern) ||
                        !AttributeValidation.IsValidRegexPattern(pattern, out _))
                    {
                        results ??= [];
                        results.Add(Diagnostic.Create(Diagnostics.RegularExpressionIsInvalid, loc, pattern));
                    }

                    if (info.ExceptionType is INamedTypeSymbol provided &&
                        exceptionSymbol != null && !provided.IsDerivedFrom(exceptionSymbol))
                    {
                        results ??= [];
                        results.Add(Diagnostic.Create(
                            Diagnostics.InvalidExceptionType,
                            loc,
                            info.ExceptionType?.Name));
                    }

                    return results?.ToImmutableArray() ?? [];
                });

        context.RegisterSourceOutput(diagnostics, static (spc, diag) => spc.ReportDiagnostic(diag));
    }
}