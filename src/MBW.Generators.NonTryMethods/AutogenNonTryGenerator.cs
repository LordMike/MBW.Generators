using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using MBW.Generators.NonTryMethods.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.NonTryMethods;

[Generator]
public sealed class AutogenNonTryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Known types by reference
        IncrementalValueProvider<KnownSymbols?> knownSymbolsProvider =
            context.CompilationProvider.Select((comp, _) => KnownSymbols.CreateInstance(comp));

        // All classes+interfaces
        IncrementalValuesProvider<INamedTypeSymbol> typesProvider = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax asType &&
                                    asType.Kind() is SyntaxKind.ClassDeclaration or SyntaxKind.InterfaceDeclaration,
                static (ctx, _) => (INamedTypeSymbol?)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node))
            .Where(static t => t is not null)
            .Select((s, _) => s!);

        // Assembly-level attributes
        IncrementalValueProvider<ImmutableArray<(GenerateNonTryMethodAttributeInfo info, Location origin)>> assemblyRuleProvider =
            knownSymbolsProvider.Combine(context.CompilationProvider)
                .Select((tuple, _) => AttributesCollection.From(tuple.Left, tuple.Right.Assembly));

        IncrementalValuesProvider<TypeSpec> perType = typesProvider.Combine(knownSymbolsProvider)
            .Combine(assemblyRuleProvider)
            .Where(static tuple =>
            {
                KnownSymbols? knownSymbols = tuple.Left.Right;
                INamedTypeSymbol typeSymbol = tuple.Left.Left;
                ImmutableArray<(GenerateNonTryMethodAttributeInfo, Location)> assemblyAttributes = tuple.Right;

                if (knownSymbols == null)
                    return false;

                // If assembly has attribute => include
                if (assemblyAttributes.Length > 0)
                    return true;

                // If type has attribute => include
                if (typeSymbol.GetAttributes()
                    .Any(a => a.AttributeClass?.Equals(knownSymbols.GenerateNonTryMethodAttribute,
                        SymbolEqualityComparer.Default) ?? false))
                    return true;

                // if type has any method with attribute => include
                if (typeSymbol.GetMembers()
                    .Any(m => m is IMethodSymbol asMethod && asMethod.GetAttributes().Any(a =>
                        a.AttributeClass?.Equals(knownSymbols.GenerateNonTryMethodAttribute,
                            SymbolEqualityComparer.Default) ?? false)))
                    return true;

                // Else, ignore
                return false;
            })
            .Select(static (tuple, _) =>
            {
                KnownSymbols knownSymbols = tuple.Left.Right!;
                INamedTypeSymbol typeSymbol = tuple.Left.Left;
                ImmutableArray<(GenerateNonTryMethodAttributeInfo info, Location origin)> assemblyAttributes = tuple.Right;
                ImmutableArray<(GenerateNonTryMethodAttributeInfo info, Location origin)> classAttributes = AttributesCollection.From(knownSymbols, typeSymbol);

                // Discover which attribute(s) applies to this type
                List<MethodSpec>? res = null;
                foreach (IMethodSymbol? method in typeSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    // Discover methods to convert in this type (based on attribute regexes, use inner most attribute first)
                    if (method.Name.Length == 0)
                        continue;

                    // Method level
                    ImmutableArray<(GenerateNonTryMethodAttributeInfo info, Location origin)> attribs =
                        AttributesCollection.From(knownSymbols, method);
                    if (attribs.Any(a => a.info.Pattern.IsMatch(method.Name)))
                    {
                        res ??= [];
                        res.Add(new MethodSpec(method, attribs));

                        continue;
                    }

                    // Class level
                    if (classAttributes.Any(a => a.info.Pattern.IsMatch(method.Name)))
                    {
                        res ??= [];
                        res.Add(new MethodSpec(method, classAttributes));

                        continue;
                    }

                    // Assembly level
                    if (assemblyAttributes.Any(a => a.info.Pattern.IsMatch(method.Name)))
                    {
                        res ??= [];
                        res.Add(new MethodSpec(method, assemblyAttributes));

                        continue;
                    }

                    // Ignore method
                }

                // If no methods => null
                if (res == null)
                    return null;

                // Emit a spec with (symbols, type, (method, info)[])
                return new TypeSpec(knownSymbols, typeSymbol, res.ToImmutableArray());
            })
            .Where(static s => s != null)
            .Select(static (s, _) => s!)
            .WithComparer(TypeSpec.Comparer);

        context.RegisterSourceOutput(perType, static (spc, spec) =>
        {
            // TODO: convert spec from selection to emit type
            // EmitType(spc, r.Type, r.Specs); // one file per type
        });
    }
}