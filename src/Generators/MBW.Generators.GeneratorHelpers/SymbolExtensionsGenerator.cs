using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MBW.Generators.GeneratorHelpers;

[Generator]
public sealed class SymbolExtensionsGenerator : IIncrementalGenerator
{
    private const string GenerateSymbolExtensionsAttributeName =
        "MBW.Generators.GeneratorHelpers.GenerateSymbolExtensionsAttribute";

    private const string SymbolNameExtensionAttributeName =
        "MBW.Generators.GeneratorHelpers.SymbolNameExtensionAttribute";

    private const string NamespaceNameExtensionAttributeName =
        "MBW.Generators.GeneratorHelpers.NamespaceNameExtensionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types = context.SyntaxProvider.ForAttributeWithMetadataName(
                GenerateSymbolExtensionsAttributeName,
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, _) => GetTypeToGenerate(ctx))
            .Where(static t => t is not null)
            .Select(static (t, _) => (TypeToGenerate)t!);

        context.RegisterSourceOutput(types, (spc, t) => Generate(spc, t));
    }

    private static TypeToGenerate? GetTypeToGenerate(GeneratorAttributeSyntaxContext ctx)
    {
        var typeSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var attr = ctx.Attributes[0];

        string? name = null;
        string? nsOverride = null;
        foreach (var kv in attr.NamedArguments)
        {
            if (kv.Key == nameof(GenerateSymbolExtensionsAttribute.Name))
                name = kv.Value.Value as string;
            else if (kv.Key == nameof(GenerateSymbolExtensionsAttribute.Namespace))
                nsOverride = kv.Value.Value as string;
        }

        var className = name ?? typeSymbol.Name + "Extensions";
        var namespaceName = nsOverride ?? "Microsoft.CodeAnalysis";
        var accessibility = typeSymbol.DeclaredAccessibility;

        var fields = new List<FieldInfo>();
        var diagnostics = new List<Diagnostic>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IFieldSymbol field)
                continue;

            foreach (var fa in field.GetAttributes())
            {
                var attrName = fa.AttributeClass?.ToDisplayString();
                FieldKind? kind = attrName switch
                {
                    SymbolNameExtensionAttributeName => FieldKind.Type,
                    NamespaceNameExtensionAttributeName => FieldKind.Namespace,
                    _ => null
                };
                if (kind is null)
                    continue;

                if (!(field.IsConst && field.Type.SpecialType == SpecialType.System_String &&
                      field.ConstantValue is string constValue && !string.IsNullOrWhiteSpace(constValue)))
                {
                    diagnostics.Add(Diagnostic.Create(Diagnostics.InvalidFieldTarget, field.Locations[0], field.Name));
                    continue;
                }

                string methodBase = GetNamedArgument(fa, "MethodName") ?? field.Name;

                if (kind == FieldKind.Type)
                {
                    if (!TryParseTypeFqn(constValue, out var typeData))
                    {
                        diagnostics.Add(Diagnostic.Create(Diagnostics.InvalidTypeFqn, field.Locations[0], field.Name,
                            constValue));
                        continue;
                    }

                    fields.Add(new FieldInfo(kind.Value, methodBase, typeData.NamespaceSegments,
                        typeData.TypeSegments, typeData.Normalized, field.Locations[0]));
                }
                else
                {
                    if (!TryParseNamespaceFqn(constValue, out var nsSegments, out var normalized))
                    {
                        diagnostics.Add(Diagnostic.Create(Diagnostics.InvalidNamespaceFqn, field.Locations[0],
                            field.Name, constValue));
                        continue;
                    }

                    fields.Add(new FieldInfo(kind.Value, methodBase, nsSegments, null, normalized,
                        field.Locations[0]));
                }
            }
        }

        if (fields.Count == 0)
        {
            return new TypeToGenerate(className, namespaceName, accessibility,
                Array.Empty<FieldToGenerate>(), diagnostics, typeSymbol.Locations[0]);
        }

        var byTarget = fields.GroupBy(f => f.NormalizedTarget);
        foreach (var group in byTarget)
        {
            bool first = true;
            foreach (var field in group)
            {
                if (first)
                {
                    first = false;
                    continue;
                }

                diagnostics.Add(Diagnostic.Create(Diagnostics.DuplicateTarget, field.Location, field.MethodBaseName,
                    group.Key));
                field.Generate = false;
            }
        }

        var byName = fields.Where(f => f.Generate).GroupBy(f => f.MethodBaseName);
        foreach (var group in byName)
        {
            int idx = 1;
            foreach (var field in group)
            {
                if (idx == 1)
                {
                    field.MethodName = field.MethodBaseName;
                }
                else
                {
                    field.MethodName = field.MethodBaseName + "_" + idx;
                    diagnostics.Add(Diagnostic.Create(Diagnostics.DuplicateMethodName, field.Location,
                        field.MethodBaseName, field.MethodName));
                }

                idx++;
            }
        }

        var fieldsToGenerate = fields.Where(f => f.Generate)
            .Select(f => new FieldToGenerate(f.Kind, f.MethodName!, f.NamespaceSegments,
                f.TypeSegments, f.Location)).ToArray();

        return new TypeToGenerate(className, namespaceName, accessibility, fieldsToGenerate,
            diagnostics, typeSymbol.Locations[0]);
    }

    private static string? GetNamedArgument(AttributeData attr, string name)
    {
        foreach (var kv in attr.NamedArguments)
            if (kv.Key == name)
                return kv.Value.Value as string;
        return null;
    }

    private static void Generate(SourceProductionContext context, TypeToGenerate type)
    {
        foreach (var diag in type.Diagnostics)
            context.ReportDiagnostic(diag);

        if (type.Fields.Length == 0)
            return;

        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System;");
        sb.AppendLine("using Microsoft.CodeAnalysis;");
        sb.AppendLine();
        sb.AppendLine($"namespace {type.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"{GetAccessibility(type.Accessibility)} static class {type.ClassName}");
        sb.AppendLine("{");

        foreach (var field in type.Fields)
        {
            if (field.Kind == FieldKind.Type)
                GenerateTypeMethod(sb, field);
            else
                GenerateNamespaceMethods(sb, field);
        }

        sb.AppendLine("}");

        context.AddSource(type.ClassName + ".g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static string GetAccessibility(Accessibility accessibility) =>
        accessibility == Accessibility.Public ? "public" : "internal";

    private static void GenerateTypeMethod(StringBuilder sb, FieldToGenerate field)
    {
        var methodSuffix = field.MethodName;

        var innermost = field.TypeSegments![field.TypeSegments.Length - 1];
        sb.AppendLine($"    public static bool IsNamedExactlyType{methodSuffix}(this ISymbol? symbol)");
        sb.AppendLine("    {");
        if (innermost.UseMetadataName)
        {
            sb.AppendLine("        if (symbol is not INamedTypeSymbol t0) return false;");
            sb.AppendLine($"        if (!t0.MetadataName.Equals(\"{innermost.Value}\", StringComparison.Ordinal)) return false;");
        }
        else
        {
            sb.AppendLine("        if (symbol is null) return false;");
            sb.AppendLine($"        if (!symbol.Name.Equals(\"{innermost.Value}\", StringComparison.Ordinal)) return false;");
            sb.AppendLine("        if (symbol is not INamedTypeSymbol t0) return false;");
        }

        string current = "t0";
        for (int i = field.TypeSegments.Length - 2, idx = 1; i >= 0; i--, idx++)
        {
            var seg = field.TypeSegments[i];
            string varName = $"t{idx}";
            sb.AppendLine($"        var {varName} = {current}.ContainingType;");
            sb.AppendLine($"        if ({varName} is null || !{varName}.{(seg.UseMetadataName ? "MetadataName" : "Name")}.Equals(\"{seg.Value}\", StringComparison.Ordinal)) return false;");
            current = varName;
        }

        sb.AppendLine($"        if ({current}.ContainingType is not null) return false;");
        sb.AppendLine($"        var ns = {current}.ContainingNamespace;");

        for (int i = field.NamespaceSegments.Length - 1; i >= 0; i--)
        {
            var seg = field.NamespaceSegments[i];
            sb.AppendLine($"        if (ns is null || !ns.Name.Equals(\"{seg}\", StringComparison.Ordinal)) return false;");
            sb.AppendLine("        ns = ns.ContainingNamespace;");
        }

        sb.AppendLine("        return ns != null && ns.IsGlobalNamespace;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateNamespaceMethods(StringBuilder sb, FieldToGenerate field)
    {
        var suffix = field.MethodName;
        var segments = field.NamespaceSegments;

        sb.AppendLine($"    public static bool IsInNamespace{suffix}(this ISymbol? symbol)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return IsInNamespace{suffix}(symbol as INamespaceSymbol ?? symbol?.ContainingNamespace);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public static bool IsExactlyInNamespace{suffix}(this ISymbol? symbol)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return IsExactlyNamespace{suffix}(symbol as INamespaceSymbol ?? symbol?.ContainingNamespace);");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine($"    public static bool IsInNamespace{suffix}(this INamespaceSymbol? ns)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (ns is null) return false;");
        sb.AppendLine("        int depth = 0;");
        sb.AppendLine("        for (var current = ns; current is not null && !current.IsGlobalNamespace; current = current.ContainingNamespace) depth++;");
        sb.AppendLine($"        if (depth < {segments.Length}) return false;");
        sb.AppendLine($"        for (int i = 0; i < depth - {segments.Length}; i++) ns = ns!.ContainingNamespace;");
        for (int i = segments.Length - 1; i >= 0; i--)
        {
            var seg = segments[i];
            sb.AppendLine($"        if (ns is null || !ns.Name.Equals(\"{seg}\", StringComparison.Ordinal)) return false;");
            sb.AppendLine("        ns = ns.ContainingNamespace;");
        }
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine($"    public static bool IsExactlyNamespace{suffix}(this INamespaceSymbol? ns)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (ns is null) return false;");
        sb.AppendLine($"        if (!ns.Name.Equals(\"{segments[segments.Length - 1]}\", StringComparison.Ordinal)) return false;");
        for (int i = segments.Length - 2; i >= 0; i--)
        {
            var seg = segments[i];
            sb.AppendLine("        ns = ns.ContainingNamespace;");
            sb.AppendLine($"        if (ns is null || !ns.Name.Equals(\"{seg}\", StringComparison.Ordinal)) return false;");
        }
        sb.AppendLine("        ns = ns.ContainingNamespace;");
        sb.AppendLine("        return ns != null && ns.IsGlobalNamespace;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static bool TryParseTypeFqn(string fqn, out TypeFqn typeFqn)
    {
        typeFqn = default;

        if (fqn.StartsWith("global::", StringComparison.Ordinal))
            fqn = fqn.Substring("global::".Length);

        int lastDot = fqn.LastIndexOf('.');
        if (lastDot < 0)
            return false;

        string nsPart = fqn.Substring(0, lastDot);
        string typePart = fqn.Substring(lastDot + 1);

        var nsSegments = nsPart.Split('.');
        if (nsSegments.Length == 0 || nsSegments.Any(s => string.IsNullOrWhiteSpace(s)))
            return false;

        var typeSegmentsRaw = typePart.Split('+');
        if (typeSegmentsRaw.Length == 0 || typeSegmentsRaw.Any(s => string.IsNullOrWhiteSpace(s)))
            return false;

        var typeSegments = typeSegmentsRaw
            .Select(s => new TypeSegment(s, s.Contains('`')))
            .ToArray();

        string normalized = string.Join(".", nsSegments) + "." + string.Join("+", typeSegmentsRaw);

        typeFqn = new TypeFqn(nsSegments, typeSegments, normalized);
        return true;
    }

    private static bool TryParseNamespaceFqn(string fqn, out string[] segments, out string normalized)
    {
        if (fqn.StartsWith("global::", StringComparison.Ordinal))
            fqn = fqn.Substring("global::".Length);

        var parts = fqn.Split('.');
        if (parts.Length == 0 || parts.Any(s => string.IsNullOrWhiteSpace(s)))
        {
            segments = Array.Empty<string>();
            normalized = string.Empty;
            return false;
        }

        segments = parts;
        normalized = string.Join(".", parts);
        return true;
    }

    private readonly record struct TypeToGenerate(
        string ClassName,
        string Namespace,
        Accessibility Accessibility,
        FieldToGenerate[] Fields,
        List<Diagnostic> Diagnostics,
        Location TypeLocation);

    private readonly record struct FieldToGenerate(
        FieldKind Kind,
        string MethodName,
        string[] NamespaceSegments,
        TypeSegment[]? TypeSegments,
        Location Location);

    private sealed class FieldInfo
    {
        public FieldInfo(FieldKind kind, string methodBaseName, string[] namespaceSegments,
            TypeSegment[]? typeSegments, string normalizedTarget, Location location)
        {
            Kind = kind;
            MethodBaseName = methodBaseName;
            NamespaceSegments = namespaceSegments;
            TypeSegments = typeSegments;
            NormalizedTarget = normalizedTarget;
            Location = location;
            Generate = true;
        }

        public FieldKind Kind { get; }
        public string MethodBaseName { get; }
        public string? MethodName { get; set; }
        public string[] NamespaceSegments { get; }
        public TypeSegment[]? TypeSegments { get; }
        public string NormalizedTarget { get; }
        public Location Location { get; }
        public bool Generate { get; set; }
    }

    private enum FieldKind
    {
        Type,
        Namespace
    }

    private readonly record struct TypeSegment(string Value, bool UseMetadataName);

    private readonly record struct TypeFqn(string[] NamespaceSegments, TypeSegment[] TypeSegments, string Normalized);
}

