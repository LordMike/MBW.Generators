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
            if (startCtx.Compilation.GetTypeByMetadataName(KnownSymbols.DefaultOverloadAttribute) is null &&
                startCtx.Compilation.GetTypeByMetadataName(KnownSymbols.TransformOverloadAttribute) is null)
                return;

            startCtx.RegisterCompilationEndAction(ctx =>
            {
                AnalyzeAttributeList(ctx.Compilation.Assembly.GetAttributes(), ctx.ReportDiagnostic);
            });

            startCtx.RegisterSymbolAction(ctx =>
                AnalyzeAttributeList(ctx.Symbol.GetAttributes(), ctx.ReportDiagnostic),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeAttributeList(
        ImmutableArray<AttributeData> attrs,
        Action<Diagnostic> report)
    {
        foreach (var attr in attrs)
        {
            if (attr.AttributeClass.IsNamedExactlyTypeDefaultOverloadAttribute())
            {
                var info = AttributeConverters.ToDefault(attr);
                ValidateRegex(info.ParameterNamePattern, attr, report);
                ValidateRegex(info.MethodNamePattern, attr, report);
            }
            else if (attr.AttributeClass.IsNamedExactlyTypeTransformOverloadAttribute())
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
