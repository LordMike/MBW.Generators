using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.OverloadGenerator;

[Generator]
public sealed class OverloadGenerator : IIncrementalGenerator
{
    private const string TransformAttributeName = "MBW.Generators.OverloadGenerator.TransformOverloadAttribute";
    private const string DefaultAttributeName = "MBW.Generators.OverloadGenerator.DefaultOverloadAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methods = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => IsCandidate(node),
            static (ctx, _) => GetMethod(ctx))
            .Where(static m => m is not null)
            .Select((m, _) => m!)
            .Collect();

        var compilationAndMethods = context.CompilationProvider.Combine(methods);

        context.RegisterSourceOutput(compilationAndMethods, static (spc, source) => Execute(spc, source.Left, source.Right));
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
        foreach (var list in lists)
            foreach (var attr in list.Attributes)
            {
                var name = attr.Name.ToString();
                if (name.Contains("TransformOverload") || name.Contains("DefaultOverload"))
                    return true;
            }
        return false;
    }

    private static MethodModel? GetMethod(GeneratorSyntaxContext context)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(methodSyntax) is not IMethodSymbol methodSymbol)
            return null;

        var rules = new List<Rule>();

        // class-level attributes
        foreach (var attr in methodSymbol.ContainingType.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == TransformAttributeName)
            {
                var rule = ParseTransform(attr);
                if (rule != null)
                    rules.Add(rule);
            }
            else if (attr.AttributeClass?.ToDisplayString() == DefaultAttributeName)
            {
                var rule = ParseDefault(attr);
                if (rule != null)
                    rules.Add(rule);
            }
        }

        // method-level attributes override
        foreach (var attr in methodSymbol.GetAttributes())
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
        var accept = attr.ConstructorArguments[1].Value as INamedTypeSymbol;
        string transform = attr.ConstructorArguments[2].Value as string ?? string.Empty;
        var usings = GetUsings(attr);
        return new TransformRule(parameter, accept, transform, usings);
    }

    private static DefaultRule? ParseDefault(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 2) return null;
        string parameter = attr.ConstructorArguments[0].Value as string ?? string.Empty;
        string expr = attr.ConstructorArguments[1].Value as string ?? string.Empty;
        var usings = GetUsings(attr);
        return new DefaultRule(parameter, expr, usings);
    }


    private static ImmutableArray<string> GetUsings(AttributeData attr)
    {
        foreach (var kv in attr.NamedArguments)
        {
            if (kv.Key == nameof(TransformOverloadAttribute.Usings) || kv.Key == nameof(DefaultOverloadAttribute.Usings))
            {
                if (kv.Value.Values is { } vals)
                    return vals.Select(v => v.Value?.ToString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToImmutableArray();
            }
        }
        return ImmutableArray<string>.Empty;
    }

    private static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<MethodModel> methods)
    {
        if (methods.IsDefaultOrEmpty)
            return;

        foreach (var group in methods.GroupBy(m => m.Method.ContainingType, SymbolEqualityComparer.Default))
        {
            var type = (INamedTypeSymbol)group.Key;
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");

            var usings = group.SelectMany(m => m.Rules).SelectMany(r => r.Usings).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().OrderBy(x => x);
            foreach (var u in usings)
                sb.Append("using ").Append(u).AppendLine(";");
            if (usings.Any())
                sb.AppendLine();

            var ns = type.ContainingNamespace;
            if (!ns.IsGlobalNamespace)
            {
                sb.Append("namespace ").Append(ns.ToDisplayString()).AppendLine();
                sb.AppendLine("{");
            }

            var types = new Stack<INamedTypeSymbol>();
            INamedTypeSymbol? current = type;
            while (current != null)
            {
                types.Push(current);
                current = current.ContainingType;
            }

            while (types.Count > 0)
            {
                var t = types.Pop();
                sb.Append("partial class ").Append(t.Name);
                if (t.TypeParameters.Length > 0)
                {
                    sb.Append('<');
                    sb.Append(string.Join(", ", t.TypeParameters.Select(tp => tp.Name)));
                    sb.Append('>');
                }
                sb.AppendLine();
                sb.AppendLine("{");
            }

            foreach (var method in group)
            {
                foreach (var rule in method.Rules)
                {
                    GenerateMethod(context, sb, method.Method, rule);
                }
            }
            // Close type blocks
            current = type;
            while (current != null)
            {
                sb.AppendLine("}");
                current = current.ContainingType;
            }

            if (!ns.IsGlobalNamespace)
                sb.AppendLine("}");

            var hint = GetHintName(type);
            context.AddSource(hint, sb.ToString());
        }
    }

    private static void GenerateMethod(SourceProductionContext context, StringBuilder sb, IMethodSymbol method, Rule rule)
    {
        int index = Array.FindIndex(method.Parameters.ToArray(), p => p.Name == rule.Parameter);
        if (index < 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingParameter, method.Locations[0], method.Name, rule.Parameter));
            return;
        }

        var trRule = rule as TransformRule;
        var drRule = rule as DefaultRule;
        if (trRule != null)
        {
            if (trRule.Accept is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidAcceptType, method.Locations[0], trRule.Parameter));
                return;
            }
            if (string.IsNullOrWhiteSpace(trRule.Transform) || !trRule.Transform.Contains("{value}"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingValueToken, method.Locations[0], trRule.Parameter));
                return;
            }
        }
        else if (drRule != null)
        {
            if (string.IsNullOrWhiteSpace(drRule.Expression))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingDefaultExpression, method.Locations[0], drRule.Parameter));
                return;
            }
        }

        var parameters = new List<string>();
        var arguments = new List<string>();
        var signature = new List<(ITypeSymbol type, RefKind kind, bool isParams)>();

        foreach (var p in method.Parameters)
        {
            if (drRule != null && p.Name == drRule.Parameter)
            {
                arguments.Add(drRule.Expression);
                continue;
            }

            var modifier = p.RefKind switch { RefKind.Ref => "ref ", RefKind.Out => "out ", RefKind.In => "in ", _ => string.Empty };
            var paramsPrefix = p.IsParams ? "params " : string.Empty;
            string typeName;
            string arg;
            if (trRule != null && p.Name == trRule.Parameter)
            {
                typeName = trRule.Accept!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                arg = modifier + trRule.Transform.Replace("{value}", p.Name);
                signature.Add((trRule.Accept!, p.RefKind, p.IsParams));
            }
            else
            {
                typeName = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                arg = modifier + p.Name;
                signature.Add((p.Type, p.RefKind, p.IsParams));
            }

            parameters.Add($"{paramsPrefix}{modifier}{typeName} {p.Name}{GetDefaultValue(p)}");
            arguments.Add(arg);
        }

        if (SignatureExists(method.ContainingType, method, signature))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SignatureCollision, method.Locations[0], method.Name));
            return;
        }

        string accessibility = GetAccessibility(method.DeclaredAccessibility);
        string modifiers = method.IsStatic ? " static" : string.Empty;
        string returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string methodName = method.Name;
        string typeParams = method.IsGenericMethod ? "<" + string.Join(", ", method.TypeParameters.Select(tp => tp.Name)) + ">" : string.Empty;
        string constraints = BuildConstraints(method);

        sb.Append("        " + accessibility + modifiers + ' ' + returnType + ' ' + methodName + typeParams)
            .Append("(").Append(string.Join(", ", parameters)).Append(")");
        if (!string.IsNullOrWhiteSpace(constraints))
        {
            sb.AppendLine();
            sb.Append("            " + constraints);
        }
        sb.AppendLine();
        sb.Append("            => " + methodName + typeParams + "(" + string.Join(", ", arguments) + ");");
        sb.AppendLine();
        sb.AppendLine();
    }
    private static bool SignatureExists(INamedTypeSymbol type, IMethodSymbol original, List<(ITypeSymbol type, RefKind kind, bool isParams)> signature)
    {
        foreach (var member in type.GetMembers(original.Name).OfType<IMethodSymbol>())
        {
            if (SymbolEqualityComparer.Default.Equals(member, original))
                continue;
            if (member.Parameters.Length != signature.Count)
                continue;

            bool match = true;
            for (int i = 0; i < signature.Count; i++)
            {
                var mp = member.Parameters[i];
                var np = signature[i];
                if (mp.RefKind != np.kind || mp.IsParams != np.isParams || !SymbolEqualityComparer.Default.Equals(mp.Type, np.type))
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return true;
        }
        return false;
    }

    private static string GetAccessibility(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Private => "private",
        Accessibility.Internal => "internal",
        Accessibility.Protected => "protected",
        Accessibility.ProtectedAndInternal => "private protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        _ => "public"
    };

    private static string BuildConstraints(IMethodSymbol method)
    {
        if (!method.IsGenericMethod)
            return string.Empty;

        var clauses = new List<string>();
        foreach (var tp in method.TypeParameters)
        {
            var c = new List<string>();
            if (tp.HasReferenceTypeConstraint)
                c.Add("class");
            if (tp.HasValueTypeConstraint)
                c.Add("struct");
            if (tp.HasUnmanagedTypeConstraint)
                c.Add("unmanaged");
            if (tp.HasNotNullConstraint)
                c.Add("notnull");
            c.AddRange(tp.ConstraintTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            if (tp.HasConstructorConstraint)
                c.Add("new()");
            if (c.Count > 0)
                clauses.Add($"where {tp.Name} : {string.Join(", ", c)}");
        }
        return string.Join(" ", clauses);
    }

    private static string GetDefaultValue(IParameterSymbol p)
    {
        if (!p.HasExplicitDefaultValue)
            return string.Empty;
        if (p.ExplicitDefaultValue == null)
            return " = null";
        return p.ExplicitDefaultValue switch
        {
            string s => " = \"" + s.Replace("\"", "\\\"") + "\"",
            bool b => " = " + (b ? "true" : "false"),
            _ => " = " + p.ExplicitDefaultValue.ToString()
        };
    }

    private static string GetHintName(INamedTypeSymbol type)
    {
        var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (name.StartsWith("global::", StringComparison.Ordinal))
            name = name.Substring(8);
        name = name.Replace('<', '_').Replace('>', '_').Replace('.', '_');
        return name + ".Overloads.g.cs";
    }

    private sealed class MethodModel
    {
        public MethodModel(IMethodSymbol method, ImmutableArray<Rule> rules)
        {
            Method = method;
            Rules = rules;
        }
        public IMethodSymbol Method { get; }
        public ImmutableArray<Rule> Rules { get; }
    }

    private abstract class Rule
    {
        protected Rule(string parameter, ImmutableArray<string> usings)
        {
            Parameter = parameter;
            Usings = usings;
        }
        public string Parameter { get; }
        public ImmutableArray<string> Usings { get; }
    }

    private sealed class TransformRule : Rule
    {
        public TransformRule(string parameter, INamedTypeSymbol? accept, string transform, ImmutableArray<string> usings)
            : base(parameter, usings)
        {
            Accept = accept;
            Transform = transform;
        }
        public INamedTypeSymbol? Accept { get; }
        public string Transform { get; }
    }

    private sealed class DefaultRule : Rule
    {
        public DefaultRule(string parameter, string expression, ImmutableArray<string> usings)
            : base(parameter, usings)
        {
            Expression = expression;
        }
        public string Expression { get; }
    }
}

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor SignatureCollision = new("OG001", "Generated signature collision", "Method '{0}' has a colliding overload", "OverloadGenerator", DiagnosticSeverity.Warning, true);
    public static readonly DiagnosticDescriptor MissingParameter = new("OG002", "Missing parameter", "Parameter '{1}' not found on method '{0}'", "OverloadGenerator", DiagnosticSeverity.Warning, true);
    public static readonly DiagnosticDescriptor InvalidAcceptType = new("OG003", "Accept type invalid", "Accept type for parameter '{0}' could not be resolved", "OverloadGenerator", DiagnosticSeverity.Warning, true);
    public static readonly DiagnosticDescriptor MissingValueToken = new("OG004", "Missing {value}", "Transform for parameter '{0}' must contain '{value}' token", "OverloadGenerator", DiagnosticSeverity.Warning, true);
    public static readonly DiagnosticDescriptor MissingDefaultExpression = new("OG005", "Default expression missing", "Default expression for parameter '{0}' is empty", "OverloadGenerator", DiagnosticSeverity.Warning, true);
}
