using System;
using System.Collections.Immutable;
using MBW.Generators.OverloadGenerator.Generator.Helpers;
using MBW.Generators.OverloadGenerator.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MBW.Generators.OverloadGenerator.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OverloadAttributeValidatorAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            Diagnostics.RegularExpressionIsInvalid,
            Diagnostics.InvalidTransformExpression
        ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static startCtx =>
        {
            var ks = KnownSymbols.TryCreateInstance(startCtx.Compilation);
            if (ks is null)
                return;

            startCtx.RegisterCompilationEndAction(ctx =>
            {
                AnalyzeAttributeList(ks, ctx.Compilation.Assembly.GetAttributes(), ctx.ReportDiagnostic);
            });

            startCtx.RegisterSymbolAction(ctx =>
                AnalyzeAttributeList(ks, ctx.Symbol.GetAttributes(), ctx.ReportDiagnostic),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeAttributeList(KnownSymbols ks,
        ImmutableArray<AttributeData> attrs,
        Action<Diagnostic> report)
    {
        foreach (var attr in attrs)
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, ks.DefaultOverloadAttribute))
            {
                var info = AttributeConverters.ToDefault(attr);
                ValidateRegex(info.ParameterNamePattern, attr, report);
                ValidateRegex(info.MethodNamePattern, attr, report);
            }
            else if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, ks.TransformOverloadAttribute))
            {
                var info = AttributeConverters.ToTransform(attr);
                ValidateRegex(info.ParameterNamePattern, attr, report);
                ValidateRegex(info.MethodNamePattern, attr, report);

                var expr = info.TransformExpression.Replace("{value}", "value");
                var syntax = SyntaxFactory.ParseExpression(expr);
                if (syntax.ContainsDiagnostics)
                {
                    var loc = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
                    report(Diagnostic.Create(Diagnostics.InvalidTransformExpression, loc, info.ParameterNamePattern, info.TransformExpression));
                }
            }
        }
    }

    private static void ValidateRegex(string pattern, AttributeData attr, Action<Diagnostic> report)
    {
        if (!AttributeValidation.IsValidRegexPattern(pattern, out _))
        {
            var loc = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
            report(Diagnostic.Create(Diagnostics.RegularExpressionIsInvalid, loc, pattern));
        }
    }
}
