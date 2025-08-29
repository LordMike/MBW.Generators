using System.Collections.Immutable;
using MBW.Generators.GeneratorHelpers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MBW.Generators.GeneratorHelpers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SymbolExtensionsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        Diagnostics.TypeMissingFields,
        Diagnostics.FieldWithoutOptIn
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        bool hasTypeAttr = false;
        bool hasFieldAttr = false;

        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == KnownSymbols.GenerateSymbolExtensionsAttributeName)
            {
                hasTypeAttr = true;
                break;
            }
        }

        foreach (var member in type.GetMembers())
        {
            if (member is not IFieldSymbol field)
                continue;

            foreach (var attr in field.GetAttributes())
            {
                var attrName = attr.AttributeClass?.ToDisplayString();
                if (attrName == KnownSymbols.SymbolNameExtensionAttributeName ||
                    attrName == KnownSymbols.NamespaceNameExtensionAttributeName)
                {
                    hasFieldAttr = true;
                    if (!hasTypeAttr)
                        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.FieldWithoutOptIn, field.Locations[0],
                            field.Name));
                    break;
                }
            }
        }

        if (hasTypeAttr && !hasFieldAttr)
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.TypeMissingFields, type.Locations[0], type.Name));
    }
}