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
            // Lift known symbols once per compilation
            var ks = KnownSymbols.TryCreateInstance(startCtx.Compilation);
            if (ks is null)
                return;

            // Resolve System.Exception once
            var exceptionBase = startCtx.Compilation.GetTypeByMetadataName("System.Exception");

            // If we can’t get the attribute or Exception, nothing to do
            if (exceptionBase is null || exceptionBase.Kind == SymbolKind.ErrorType)
                return;

            // 1) Validate assembly-level attributes
            startCtx.RegisterCompilationEndAction(context =>
            {
                AnalyzeAttributeList(
                    ks,
                    context.Compilation.Assembly.GetAttributes(),
                    exceptionBase,
                    context.ReportDiagnostic);
            });

            // 2) Validate attributes on symbols (types/methods/fields/props/events/params, etc.)
            // Register a single symbol action that covers common attribute targets.
            startCtx.RegisterSymbolAction(
                context => AnalyzeAttributeList(ks, context.Symbol.GetAttributes(), exceptionBase,
                    context.ReportDiagnostic),
                SymbolKind.NamedType
            );
        });
    }

    private static void AnalyzeAttributeList(KnownSymbols ks,
        ImmutableArray<AttributeData> attrs,
        INamedTypeSymbol exceptionBase,
        Action<Diagnostic> report)
    {
        foreach (var attr in attrs)
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, ks.GenerateNonTryMethodAttribute))
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
                !IsDerivedFrom(provided, exceptionBase))
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

    private static bool IsDerivedFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var cur = type; cur is not null; cur = cur.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(cur, baseType))
                return true;
        }

        return false;
    }
}