using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using MBW.Generators.OverloadGenerator.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.OverloadGenerator;

[Generator]
public sealed class OverloadGenerator : IIncrementalGenerator
{
    private const string TransformAttributeName = "MBW.Generators.OverloadGenerator.Attributes.TransformOverloadAttribute";
    private const string DefaultAttributeName = "MBW.Generators.OverloadGenerator.Attributes.DefaultOverloadAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<MethodModel>> methods = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsCandidate(node),
                static (ctx, _) => GetMethod(ctx))
            .Where(static m => m is not null)
            .Select((m, _) => m!)
            .Collect();

        IncrementalValueProvider<(Compilation Left, ImmutableArray<MethodModel> Right)> compilationAndMethods = context.CompilationProvider.Combine(methods);

        context.RegisterSourceOutput(compilationAndMethods,
            static (spc, source) => Execute(spc, source.Left, source.Right));
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
        if (context.SemanticModel.GetDeclaredSymbol(methodSyntax) is not IMethodSymbol methodSymbol)
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
        ImmutableArray<string> usings = GetUsings(attr);
        return new TransformRule(parameter, accept, transform, usings);
    }

    private static DefaultRule? ParseDefault(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length < 2) return null;
        string parameter = attr.ConstructorArguments[0].Value as string ?? string.Empty;
        string expr = attr.ConstructorArguments[1].Value as string ?? string.Empty;
        ImmutableArray<string> usings = GetUsings(attr);
        return new DefaultRule(parameter, expr, usings);
    }


    private static ImmutableArray<string> GetUsings(AttributeData attr)
    {
        foreach (KeyValuePair<string, TypedConstant> kv in attr.NamedArguments)
        {
            if (kv.Key == nameof(TransformOverloadAttribute.Usings) ||
                kv.Key == nameof(DefaultOverloadAttribute.Usings))
            {
                if (kv.Value.Values is { } vals)
                    return vals.Select(v => v.Value?.ToString() ?? string.Empty)
                        .Where(s => !string.IsNullOrWhiteSpace(s)).ToImmutableArray();
            }
        }

        return ImmutableArray<string>.Empty;
    }

    private static void Execute(SourceProductionContext context, Compilation compilation,
        ImmutableArray<MethodModel> methods)
    {
        if (methods.IsDefaultOrEmpty)
            return;

        foreach (IGrouping<ISymbol?, MethodModel>? group in methods.GroupBy(m => m.Method.ContainingType, SymbolEqualityComparer.Default))
        {
            INamedTypeSymbol? type = (INamedTypeSymbol)group.Key;
            string fileText = BuildFileForType(context, type, group);
            string hint = GetHintName(type);
            context.AddSource(hint, fileText);
        }
    }

    private static string BuildFileForType(
        SourceProductionContext context,
        INamedTypeSymbol type,
        IEnumerable<MethodModel> group)
    {
        // Collect distinct, sorted usings from all rules
        string[] usings = group
            .SelectMany(m => m.Rules)
            .SelectMany(r => r.Usings)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct()
            .OrderBy(u => u)
            .ToArray();

        string usingSection = string.Join("\n", usings.Select(u => $"using {u};"));
        INamespaceSymbol? ns = type.ContainingNamespace;

        (string typeOpeners, string typeClosers, string methodIndent) = BuildTypeBlocks(type, ns);
        StringBuilder methodsText = new StringBuilder();

        foreach (MethodModel? method in group)
        {
            foreach (Rule? rule in method.Rules)
            {
                string? generated = GenerateMethodSource(context, method.Method, rule, methodIndent);
                if (generated is not null)
                    methodsText.Append(generated);
            }
        }

        string nsOpen = ns.IsGlobalNamespace ? "" : $"namespace {ns.ToDisplayString()}\n{{";
        string nsClose = ns.IsGlobalNamespace ? "" : "}";

        // One big, readable file as a raw string
        return $"""
                // <auto-generated/>
                {(usingSection.Length > 0 ? usingSection + "\n\n" : "")}
                {nsOpen}
                {typeOpeners}
                {methodsText}
                {typeClosers}
                {nsClose}
                """;
    }

    private static string? GenerateMethodSource(
        SourceProductionContext context,
        IMethodSymbol method,
        Rule rule,
        string indent)
    {
        int index = Array.FindIndex(method.Parameters.ToArray(), p => p.Name == rule.Parameter);
        if (index < 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingParameter, method.Locations[0], method.Name,
                rule.Parameter));
            return null;
        }

        TransformRule? trRule = rule as TransformRule;
        DefaultRule? drRule = rule as DefaultRule;

        if (trRule is not null)
        {
            if (trRule.Accept is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidAcceptType, method.Locations[0],
                    trRule.Parameter));
                return null;
            }

            if (string.IsNullOrWhiteSpace(trRule.Transform) || !trRule.Transform.Contains("{value}"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingValueToken, method.Locations[0],
                    trRule.Parameter));
                return null;
            }
        }
        else if (drRule is not null)
        {
            if (string.IsNullOrWhiteSpace(drRule.Expression))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingDefaultExpression, method.Locations[0],
                    drRule.Parameter));
                return null;
            }
        }

        List<string> parameters = new List<string>();
        List<string> arguments = new List<string>();
        List<(ITypeSymbol type, RefKind kind, bool isParams)> signature = new List<(ITypeSymbol type, RefKind kind, bool isParams)>();

        foreach (IParameterSymbol? p in method.Parameters)
        {
            if (drRule != null && p.Name == drRule.Parameter)
            {
                arguments.Add(drRule.Expression);
                continue;
            }

            string modifier = p.RefKind switch
            {
                RefKind.Ref => "ref ", RefKind.Out => "out ", RefKind.In => "in ", _ => string.Empty
            };
            string paramsPrefix = p.IsParams ? "params " : string.Empty;

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
            context.ReportDiagnostic(
                Diagnostic.Create(Diagnostics.SignatureCollision, method.Locations[0], method.Name));
            return null;
        }

        string accessibility = GetAccessibility(method.DeclaredAccessibility);
        string modifiers = method.IsStatic ? " static" : string.Empty;
        string returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string methodName = method.Name;
        string typeParams = method.IsGenericMethod
            ? "<" + string.Join(", ", method.TypeParameters.Select(tp => tp.Name)) + ">"
            : string.Empty;
        string constraints = BuildConstraints(method);

        string paramList = string.Join(", ", parameters);
        string argList = string.Join(", ", arguments);

        // Put constraints on the following line if present
        string constraintsText = string.IsNullOrWhiteSpace(constraints)
            ? ""
            : $"\n{indent}    {constraints}";

        return $"""
                {indent}{accessibility}{modifiers} {returnType} {methodName}{typeParams}({paramList}){constraintsText}
                {indent}    => {methodName}{typeParams}({argList});

                """;
    }

    private static (string openers, string closers, string methodIndent) BuildTypeBlocks(
        INamedTypeSymbol type,
        INamespaceSymbol ns)
    {
        List<INamedTypeSymbol> types = GetTypeChain(type); // outermost -> innermost
        int nsIndent = ns.IsGlobalNamespace ? 0 : 1;

        StringBuilder open = new StringBuilder();
        for (int i = 0; i < types.Count; i++)
        {
            INamedTypeSymbol? t = types[i];
            string indent = new string(' ', 4 * (nsIndent + i));
            string typeParams = t.TypeParameters.Length > 0
                ? "<" + string.Join(", ", t.TypeParameters.Select(tp => tp.Name)) + ">"
                : string.Empty;

            open.AppendLine($"{indent}partial class {t.Name}{typeParams}");
            open.AppendLine($"{indent}{{");
        }

        StringBuilder close = new StringBuilder();
        for (int i = types.Count - 1; i >= 0; i--)
        {
            string indent = new string(' ', 4 * (nsIndent + i));
            close.AppendLine($"{indent}}}");
        }

        string methodIndent = new string(' ', 4 * (nsIndent + types.Count)); // one level deeper than last type
        return (open.ToString(), close.ToString(), methodIndent);
    }

    private static List<INamedTypeSymbol> GetTypeChain(INamedTypeSymbol type)
    {
        Stack<INamedTypeSymbol> stack = new Stack<INamedTypeSymbol>();
        INamedTypeSymbol? current = type;
        while (current is not null)
        {
            stack.Push(current);
            current = current.ContainingType;
        }

        return stack.ToList(); // now outermost -> innermost
    }

    private static bool SignatureExists(INamedTypeSymbol type, IMethodSymbol original,
        List<(ITypeSymbol type, RefKind kind, bool isParams)> signature)
    {
        foreach (IMethodSymbol? member in type.GetMembers(original.Name).OfType<IMethodSymbol>())
        {
            if (SymbolEqualityComparer.Default.Equals(member, original))
                continue;
            if (member.Parameters.Length != signature.Count)
                continue;

            bool match = true;
            for (int i = 0; i < signature.Count; i++)
            {
                IParameterSymbol mp = member.Parameters[i];
                (ITypeSymbol type, RefKind kind, bool isParams) np = signature[i];
                if (mp.RefKind != np.kind || mp.IsParams != np.isParams ||
                    !SymbolEqualityComparer.Default.Equals(mp.Type, np.type))
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

        List<string> clauses = new List<string>();
        foreach (ITypeParameterSymbol? tp in method.TypeParameters)
        {
            List<string> c = new List<string>();
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
        string name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
        public TransformRule(string parameter, INamedTypeSymbol? accept, string transform,
            ImmutableArray<string> usings)
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