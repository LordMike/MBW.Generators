using System;
using System.Collections.Immutable;
using MBW.Generators.NonTryMethods.Generator.Helpers;
using MBW.Generators.NonTryMethods.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MBW.Generators.NonTryMethods.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonTryAttributeValidatorAnalyzer : DiagnosticAnalyzer
{
    // Reuse your existing DiagnosticDescriptors
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        Diagnostics.RegularExpressionIsInvalid,
        Diagnostics.InvalidExceptionType
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();

        // We only care about user code; generated files can be noisy
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static startCtx =>
        {
            if (startCtx.Compilation.GetTypeByMetadataName(KnownSymbols.GenerateNonTryMethodAttribute) is null)
                return;

            startCtx.RegisterCompilationEndAction(context =>
            {
                AnalyzeAttributeList(
                    context.Compilation.Assembly.GetAttributes(),
                    context.ReportDiagnostic);
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
            if (!attr.AttributeClass.IsNamedExactlyTypeGenerateNonTryMethodAttribute())
                continue;

            // Reuse your converter; it already understands your attribute shape
            var info = AttributeConverters.ToNonTry(in attr);
            var pattern = info.MethodNamePattern;

            // Regex validation
            if (string.IsNullOrWhiteSpace(pattern) ||
                !AttributeValidation.IsValidRegexPattern(pattern, out _))
            {
                var loc = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                          ?? Location.None;

                report(Diagnostic.Create(
                    Diagnostics.RegularExpressionIsInvalid,
                    loc,
                    pattern));
            }

            // Exception type validation (only when a type argument was provided)
            if (info.ExceptionType is INamedTypeSymbol provided &&
                !IsDerivedFromException(provided))
            {
                var loc = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                          ?? Location.None;

                report(Diagnostic.Create(
                    Diagnostics.InvalidExceptionType,
                    loc,
                    provided.Name));
            }
        }
    }

    private static bool IsDerivedFromException(INamedTypeSymbol type)
    {
        for (var cur = type; cur is not null; cur = cur.BaseType)
        {
            if (cur.IsNamedExactlyTypeExceptionBase())
                return true;
        }

        return false;
    }
}