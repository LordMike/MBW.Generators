using System.Collections.Generic;
using System.Collections.Immutable;
using MBW.Generators.Common;
using MBW.Generators.OverloadGenerator.Attributes;
using MBW.Generators.OverloadGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.OverloadGenerator;

[Generator]
public sealed class OverloadGenerator : GeneratorBase<OverloadGenerator>
{
    private const string TransformAttributeName =
        "MBW.Generators.OverloadGenerator.Attributes.TransformOverloadAttribute";

    private const string DefaultAttributeName = "MBW.Generators.OverloadGenerator.Attributes.DefaultOverloadAttribute";

    protected override void InitializeInternal(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<MethodModel>> methods = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsCandidate(node),
                static (ctx, _) => GetMethod(ctx))
            .Where(static m => m is not null)
            .Select((m, _) => m!)
            .Collect();

        IncrementalValueProvider<(Compilation Left, ImmutableArray<MethodModel> Right)> compilationAndMethods =
            context.CompilationProvider.Combine(methods);

        context.RegisterSourceOutput(compilationAndMethods,
            static (spc, source) => OverloadCodeGen.Execute(spc, source.Right));
    }

    private static bool IsCandidate(SyntaxNode node)
    {
        if (node is MethodDeclarationSyntax m)
        {
            if (HasOurAttribute(m.AttributeLists))
                return true;
            if (m.Parent is TypeDeclarationSyntax t && HasOurAttribute(t.AttributeLists))
                return true;
        }

        return false;
    }

    private static bool HasOurAttribute(SyntaxList<AttributeListSyntax> lists)
    {
        foreach (AttributeListSyntax list in lists)
        foreach (AttributeSyntax attr in list.Attributes)
        {
            string name = attr.Name.ToString();
            if (name.Contains("TransformOverload") || name.Contains("DefaultOverload"))
                return true;
        }

        return false;
    }

    private static MethodModel? GetMethod(GeneratorSyntaxContext context)
    {
        MethodDeclarationSyntax methodSyntax = (MethodDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(methodSyntax) is not { } methodSymbol)
            return null;

        List<Rule> rules = new List<Rule>();

        // class-level attributes
        foreach (AttributeData? attr in methodSymbol.ContainingType.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == TransformAttributeName)
            {
                TransformRule? rule = ParseTransform(attr);
                if (rule != null)
                    rules.Add(rule);
            }
            else if (attr.AttributeClass?.ToDisplayString() == DefaultAttributeName)
            {
                DefaultRule? rule = ParseDefault(attr);
                if (rule != null)
                    rules.Add(rule);
            }
        }

        // method-level attributes override
        foreach (AttributeData? attr in methodSymbol.GetAttributes())
        {
            Rule? rule = null;
            if (attr.AttributeClass?.ToDisplayString() == TransformAttributeName)
                rule = ParseTransform(attr);
            else if (attr.AttributeClass?.ToDisplayString() == DefaultAttributeName)
                rule = ParseDefault(attr);

            if (rule != null)
            {
                rules.RemoveAll(r => r.GetType() == rule.GetType() && r.Parameter == rule.Parameter);
                rules.Add(rule);
            }
        }

        if (rules.Count == 0)
            return null;

        return new MethodModel(methodSymbol, ImmutableArray.CreateRange(rules));
    }

    private static TransformRule? ParseTransform(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 3) return null;
        string parameter = attr.ConstructorArguments[0].Value as string ?? string.Empty;
        INamedTypeSymbol? accept = attr.ConstructorArguments[1].Value as INamedTypeSymbol;
        string transform = attr.ConstructorArguments[2].Value as string ?? string.Empty;
        TypeNullability nullability = TypeNullability.NotNullable;
        if (attr.ConstructorArguments.Length > 3 && attr.ConstructorArguments[3].Value is int n)
            nullability = (TypeNullability)n;

        return new TransformRule(parameter, accept, transform, nullability);
    }

    private static DefaultRule? ParseDefault(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 2) return null;
        string parameter = attr.ConstructorArguments[0].Value as string ?? string.Empty;
        string expr = attr.ConstructorArguments[1].Value as string ?? string.Empty;
        return new DefaultRule(parameter, expr);
    }
}