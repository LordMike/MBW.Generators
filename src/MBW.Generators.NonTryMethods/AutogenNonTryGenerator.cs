using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using MBW.Generators.NonTryMethods.Attributes;
using MBW.Generators.NonTryMethods.GenerationModels;
using MBW.Generators.NonTryMethods.Helpers;
using MBW.Generators.NonTryMethods.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
                                    asType.Kind() is SyntaxKind.ClassDeclaration or SyntaxKind.InterfaceDeclaration
                                        or SyntaxKind.StructDeclaration,
                static (ctx, _) => (INamedTypeSymbol?)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node))
            .Where(static t => t is not null)
            .Select((s, _) => s!);

        // Assembly-level attributes
        IncrementalValueProvider<ImmutableArray<GenerateNonTryMethodAttributeInfo>>
            assemblyRuleProvider =
                knownSymbolsProvider.Combine(context.CompilationProvider)
                    .Select((tuple, _) => AttributesCollection.From(tuple.Left, tuple.Right.Assembly));

        IncrementalValuesProvider<TypeSpec> perType = typesProvider.Combine(knownSymbolsProvider)
            .Combine(assemblyRuleProvider)
            .Where(static tuple =>
            {
                KnownSymbols? knownSymbols = tuple.Left.Right;
                INamedTypeSymbol typeSymbol = tuple.Left.Left;
                ImmutableArray<GenerateNonTryMethodAttributeInfo> assemblyAttributes = tuple.Right;

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

                // Else, ignore
                return false;
            })
            .Select(static (tuple, _) =>
            {
                KnownSymbols knownSymbols = tuple.Left.Right!;
                INamedTypeSymbol typeSymbol = tuple.Left.Left;
                ImmutableArray<GenerateNonTryMethodAttributeInfo> assemblyAttributes =
                    tuple.Right;
                ImmutableArray<GenerateNonTryMethodAttributeInfo> classAttributes =
                    AttributesCollection.From(knownSymbols, typeSymbol);

                // Discover which attribute(s) applies to this type
                List<MethodSpec>? res = null;
                foreach (IMethodSymbol? method in typeSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    // Discover methods to convert in this type (based on attribute regexes, use inner most attribute first)
                    if (method.Name.Length == 0)
                        continue;

                    // Class level
                    if (classAttributes.Any(a => a.Pattern.IsMatch(method.Name)))
                    {
                        res ??= [];
                        res.Add(new MethodSpec(method, classAttributes));

                        continue;
                    }

                    // Assembly level
                    if (assemblyAttributes.Any(a => a.Pattern.IsMatch(method.Name)))
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
            .Select(static (s, _) => s!);

        context.RegisterSourceOutput(perType, static (spc, spec) =>
        {
            // 1) Decide strategy for this type
            var plan = DetermineTypeStrategy(spec);

            // 2) Plan all candidate methods (per-method decisions + diagnostics on failures)
            var planned = PlanAllMethods(spc, spec, plan);

            // 3) Filter duplicates/collisions
            var filtered = FilterCollisionsAndDuplicates(spc, spec, plan, planned);

            // 4) Build CU, normalize, emit
            if (filtered.Length == 0) return;
            var cu = BuildCompilationUnit(spec, plan, filtered);
            var text = cu.NormalizeWhitespace().ToFullString();
            spc.AddSource(GetHintName(spec.Type), SourceText.From(text, Encoding.UTF8));
        });
    }

    static IEnumerable<PlannedMethod> PlanAllMethods(SourceProductionContext spc, TypeSpec spec, TypeEmissionPlan plan)
    {
        foreach (var m in spec.Methods)
            if (TryPlanMethod(spc, spec, plan, m, out var planned))
                yield return planned;
    }

    private static string GetHintName(INamedTypeSymbol type)
    {
        // global::A.B.Outer.Inner`1 -> A.B.Outer.Inner.NonTry.g.cs (strip arity on leaf)
        var fqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        const string globalPrefix = "global::";
        if (fqn.StartsWith(globalPrefix, StringComparison.Ordinal))
            fqn = fqn.Substring(globalPrefix.Length);

        // Leaf simple name without arity suffix
        var simple = type.Name;
        var tick = simple.IndexOf('`');
        if (tick >= 0)
            simple = simple.Substring(0, tick);

        // Namespace (if any)
        string ns = "";
        var nsSym = type.ContainingNamespace;
        if (nsSym is not null && !nsSym.IsGlobalNamespace)
        {
            ns = nsSym.ToDisplayString();
            if (ns.Length != 0)
                ns += ".";
        }

        return ns + simple + ".NonTry.g.cs";
    }


    /// <summary>
    /// Build a full compilation unit for the given type + planned methods.
    /// Caller should do: cu.NormalizeWhitespace().ToFullString() then AddSource.
    /// </summary>
    private static CompilationUnitSyntax BuildCompilationUnit(
        TypeSpec spec,
        TypeEmissionPlan plan,
        ImmutableArray<PlannedMethod> planned)
    {
        // Header + nullable
        var headerTrivia = TriviaList(
            Comment("// <auto-generated/>"),
            CarriageReturnLineFeed,
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), isActive: true)),
            CarriageReturnLineFeed
        );

        // Usings (add Tasks only if needed)
        bool needsTasks = planned.Any(pm => pm.IsAsync);
        var usingList = new List<UsingDirectiveSyntax>
        {
            UsingDirective(IdentifierName("System"))
        };
        if (needsTasks)
        {
            usingList.Add(
                UsingDirective(
                    QualifiedName(
                        QualifiedName(IdentifierName("System"), IdentifierName("Threading")),
                        IdentifierName("Tasks"))));
        }

        // Namespace (file-scoped if present)
        var nsName = spec.Type.ContainingNamespace.RenderNamespaceName();
        MemberDeclarationSyntax container = BuildTypeContainer(spec, plan, planned);

        CompilationUnitSyntax cu;
        if (nsName is null)
        {
            cu = CompilationUnit()
                .WithUsings(List(usingList))
                .WithMembers(SingletonList(container));
        }
        else
        {
            // file-scoped namespace keeps indentation minimal
            var fileNs = FileScopedNamespaceDeclaration(nsName)
                .WithMembers(SingletonList(container));

            cu = CompilationUnit()
                .WithUsings(List(usingList))
                .WithMembers(SingletonList<MemberDeclarationSyntax>(fileNs));
        }

        // Attach header + nullable and return
        return cu.WithLeadingTrivia(headerTrivia);
    }

    private static TypeEmissionPlan DetermineTypeStrategy(TypeSpec spec)
    {
        var opts = GetEffectiveOptions(spec);
        var type = spec.Type;

        bool isInterface = type.TypeKind == TypeKind.Interface;
        bool isPartial = type.IsPartial();
        bool supportsIfaceDefaults = true; // assume C# 8+

        // Emit-anyway policy: prefer in-place for Auto, even if not partial.
        var strategy = opts.MethodsGenerationStrategy == MethodsGenerationStrategy.Auto
            ? MethodsGenerationStrategy.PartialType
            : opts.MethodsGenerationStrategy;

        // We still report capabilities for anyone who cares, but we won't use them to block emission.
        return new TypeEmissionPlan(
            type,
            strategy,
            canHostPartials: isPartial || isInterface, // informational only
            isInterface: isInterface,
            supportsInterfaceDefaults: supportsIfaceDefaults);
    }

    private static MethodClassification ClassifyMethodShape(
        IMethodSymbol method,
        GenerateNonTryOptionsAttributeInfo opts,
        KnownSymbols ks)
    {
        // --- 1) Sync: bool return + exactly one 'out' parameter ---
        if (method.ReturnType.SpecialType == SpecialType.System_Boolean)
        {
            IParameterSymbol? outParam = null;
            foreach (var p in method.Parameters)
            {
                if (p.RefKind == RefKind.Out)
                {
                    if (outParam != null)
                    {
                        // More than one 'out' → not eligible 
                        return new MethodClassification(MethodShape.NotEligible, null, isValueTask: false,
                            payloadType: null);
                    }

                    outParam = p;
                }
            }

            if (outParam != null)
                return new MethodClassification(MethodShape.SyncBoolOut, outParam, isValueTask: false,
                    payloadType: null);

            // bool return but no 'out' → not eligible
            return new MethodClassification(MethodShape.NotEligible, null, isValueTask: false, payloadType: null);
        }

        // --- 2) Async: Task<(bool,T)> or ValueTask<(bool,T)> ---
        if ((opts.AsyncCandidateStrategy & AsyncCandidateStrategy.TupleBooleanAndValue) != 0)
        {
            if (method.ReturnType is INamedTypeSymbol rt && rt.IsGenericType)
            {
                // Task<T>
                if (ks.TaskOfT is not null &&
                    SymbolEqualityComparer.Default.Equals(rt.OriginalDefinition, ks.TaskOfT))
                {
                    var tArg = rt.TypeArguments[0];
                    if (TryGetTupleBoolPayload(tArg, out var payload))
                        return new MethodClassification(MethodShape.AsyncTuple, null, isValueTask: false,
                            payloadType: payload);
                }

                // ValueTask<T>
                if (ks.ValueTaskOfT is not null &&
                    SymbolEqualityComparer.Default.Equals(rt.OriginalDefinition, ks.ValueTaskOfT))
                {
                    var tArg = rt.TypeArguments[0];
                    if (TryGetTupleBoolPayload(tArg, out var payload))
                        return new MethodClassification(MethodShape.AsyncTuple, null, isValueTask: true,
                            payloadType: payload);
                }
            }
        }

        // --- 3) Not eligible ---
        return new MethodClassification(MethodShape.NotEligible, null, isValueTask: false, payloadType: null);

        static bool TryGetTupleBoolPayload(ITypeSymbol t, out ITypeSymbol? payload)
        {
            payload = null;

            if (t is INamedTypeSymbol nt && nt.IsTupleType)
            {
                var elems = nt.TupleElements;
                if (elems.Length == 2 && elems[0].Type.SpecialType == SpecialType.System_Boolean)
                {
                    payload = elems[1].Type;
                    return true;
                }

                return false;
            }

            if (t is INamedTypeSymbol nt2 && nt2.Arity == 2)
            {
                // System.ValueTuple`2<bool, T>
                if (nt2.MetadataName.StartsWith("ValueTuple`2", StringComparison.Ordinal) &&
                    IsInSystemNamespace(nt2.ContainingNamespace))
                {
                    var args = nt2.TypeArguments;
                    if (args.Length == 2 && args[0].SpecialType == SpecialType.System_Boolean)
                    {
                        payload = args[1];
                        return true;
                    }
                }
            }

            return false;

            static bool IsInSystemNamespace(INamespaceSymbol? ns)
            {
                while (ns is not null && !ns.IsGlobalNamespace)
                {
                    if (ns.Name == "System" && (ns.ContainingNamespace?.IsGlobalNamespace ?? true))
                        return true;
                    ns = ns.ContainingNamespace;
                }

                return false;
            }
        }
    }

    private static bool TryPlanMethod(
        SourceProductionContext spc,
        TypeSpec typeSpec,
        TypeEmissionPlan plan,
        MethodSpec methodSpec,
        out PlannedMethod planned)
    {
        planned = default;

        var method = methodSpec.Method;
        var ks = typeSpec.Symbols;

        // 1) find matching NonTry patterns (type/assembly were already filtered into ApplicableAttributes)
        GenerateNonTryMethodAttributeInfo attrib;
        {
            var matches = methodSpec.ApplicableAttributes
                .Where(a => a.Pattern.IsMatch(method.Name))
                .ToArray();

            if (matches.Length == 0)
                throw new InvalidOperationException("ERROR - expected method to have 1+ matching attributes");
            if (matches.Length >= 2)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MultiplePatternsMatchMethod,
                    method.Locations.FirstOrDefault(),
                    method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    method.Name,
                    typeSpec.Type.Name,
                    string.Join(", ", matches.Select(s => $"'{s.Pattern}'"))));
            }

            attrib = matches[0];
        }

        // Effective options (type > assembly > defaults)
        var opts = GetEffectiveOptions(typeSpec);

        // 2) classify shape
        var cls = ClassifyMethodShape(method, opts, ks);
        if (cls.Shape == MethodShape.NotEligible)
        {
            // Heuristic: if returns bool or has any out param -> treat as sync mismatch; else async mismatch
            var looksSync = method.ReturnType.SpecialType == SpecialType.System_Boolean ||
                            method.Parameters.Any(p => p.RefKind == RefKind.Out);
            if (looksSync)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.NotEligibleSync,
                    method.Locations.FirstOrDefault(),
                    method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    // use first pattern text for message
                    attrib.MethodNamePattern));
            }
            else
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.NotEligibleAsyncShape,
                    method.Locations.FirstOrDefault(),
                    method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    attrib.MethodNamePattern,
                    opts.AsyncCandidateStrategy));
            }

            return false;
        }

        // 3) compute generated name
        string newName = ComputeGeneratedName(method.Name, attrib);

        // 4) resolve exception type
        var exType = ResolveExceptionType(spc, ks, attrib);

        // 5) choose emission kind
        EmissionKind kind =
            plan.Strategy == MethodsGenerationStrategy.Extensions ? EmissionKind.Extension
            : plan.IsInterface ? EmissionKind.InterfaceDefault
            : EmissionKind.Partial;

        // Extensions on static methods are generally meaningless
        if (kind == EmissionKind.Extension && method.IsStatic)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.UnableToGenerateExtensionMethodForStaticMethod,
                method.Locations.FirstOrDefault(),
                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                method.Name,
                typeSpec.Type.Name));
            return false;
        }

        // 6) build final signature pieces
        bool isAsync = cls.Shape == MethodShape.AsyncTuple;
        bool isValueTask = cls.IsValueTask;

        // Return type
        ITypeSymbol returnType;
        ImmutableArray<IParameterSymbol> parameters;

        if (!isAsync)
        {
            // sync: return = out param type (maybe adjusted); parameters = all minus that out
            var outP = cls.OutParam!;
            returnType = AdjustForReturnStrategy(outP.Type, opts.ReturnGenerationStrategy);
            parameters = method.Parameters.Where(p => !SymbolEqualityComparer.Default.Equals(p, outP))
                .ToImmutableArray();
        }
        else
        {
            // async: return = Task<T> / ValueTask<T>, where T is payload (maybe adjusted). params unchanged.
            var payload = AdjustForReturnStrategy(cls.PayloadType!, opts.ReturnGenerationStrategy);

            INamedTypeSymbol wrapper = isValueTask ? ks.ValueTaskOfT : ks.TaskOfT;
            var constructed = wrapper.Construct(payload);
            returnType = constructed;
            parameters = method.Parameters.ToImmutableArray();
        }

        // IsStatic of generated method:
        // - Partial: mirror source.IsStatic
        // - InterfaceDefault: force instance (non-static)
        // - Extension: static (receiver added during emission)
        bool genIsStatic = kind switch
        {
            EmissionKind.Partial => method.IsStatic,
            EmissionKind.InterfaceDefault => false,
            EmissionKind.Extension => true,
            _ => method.IsStatic
        };

        var sig = new PlannedSignature(kind, newName, returnType, parameters, genIsStatic);
        planned = new PlannedMethod(methodSpec, sig, exType, isAsync, isValueTask);
        return true;

        static string ComputeGeneratedName(string original, GenerateNonTryMethodAttributeInfo info)
        {
            var m = info.Pattern.Match(original);
            if (m.Success)
            {
                // Prefer first capture group if present
                if (m.Groups.Count > 1 && m.Groups[1].Success)
                    return m.Groups[1].Value;
            }

            // Fallback: strip leading Try/try if present; else keep name unchanged
            if (original.StartsWith("Try", StringComparison.Ordinal))
                return original.Substring(3);
            if (original.StartsWith("try", StringComparison.Ordinal))
                return original.Substring(3);
            return original;
        }

        static ITypeSymbol AdjustForReturnStrategy(ITypeSymbol t, ReturnGenerationStrategy strat)
        {
            if (strat == ReturnGenerationStrategy.Verbatim) return t;

            // TrueMeansNotNull:
            // If Nullable<T> => unwrap
            if (t is INamedTypeSymbol nt && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                return nt.TypeArguments[0];

            // If reference type and annotated, drop annotation
            if (t.NullableAnnotation == NullableAnnotation.Annotated &&
                (t.IsReferenceType || t.IsAnonymousType))
            {
                return t.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            }

            return t;
        }
    }

    private static ImmutableArray<PlannedMethod> FilterCollisionsAndDuplicates(
        SourceProductionContext spc,
        TypeSpec typeSpec,
        TypeEmissionPlan plan,
        IEnumerable<PlannedMethod> candidates)
    {
        // 1) Build signature keys; dedupe identical -> NT006.
        var seen = new Dictionary<SignatureKey, PlannedMethod>();
        var deduped = new List<PlannedMethod>();

        foreach (var pm in candidates)
        {
            var key = SignatureKey.From(pm);
            if (seen.ContainsKey(key))
            {
                // Duplicate generated signature in this type
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.DuplicateGeneratedSignature,
                    pm.Source.Method.Locations.FirstOrDefault(),
                    pm.Signature.Name,
                    typeSpec.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                continue; // keep first, drop duplicate
            }

            seen[key] = pm;
            deduped.Add(pm);
        }

        // 2) Check against existing members; collisions -> NT005.
        // Only relevant when emitting into the target type (partials/interface defaults).
        if (deduped.Count == 0) return ImmutableArray<PlannedMethod>.Empty;

        if (deduped.Any(p => p.Signature.Kind != EmissionKind.Extension))
        {
            // Build a set of existing method signatures in the target type for quick collision checks
            var existing = new HashSet<SignatureKey>();
            foreach (var m in typeSpec.Type.GetMembers().OfType<IMethodSymbol>())
            {
                if (m.MethodKind != MethodKind.Ordinary) continue;
                existing.Add(SignatureKey.FromExisting(m));
            }

            // Filter out planned methods that collide with existing members
            var filtered = new List<PlannedMethod>(deduped.Count);
            foreach (var pm in deduped)
            {
                if (pm.Signature.Kind == EmissionKind.Extension)
                {
                    // Extensions go to a separate container; existing-member collision doesn't apply here.
                    filtered.Add(pm);
                    continue;
                }

                var genKey = SignatureKey.From(pm);
                if (existing.Contains(genKey))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.SignatureCollision,
                        pm.Source.Method.Locations.FirstOrDefault(),
                        pm.Signature.Name,
                        typeSpec.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                    continue; // drop colliding generated member
                }

                filtered.Add(pm);
            }

            return filtered.ToImmutableArray();
        }

        // Only extensions were present; duplicates already handled
        return deduped.ToImmutableArray();
    }

    private static GenerateNonTryOptionsAttributeInfo GetEffectiveOptions(TypeSpec spec)
    {
        var ks = spec.Symbols;
        var type = spec.Type;

        // 1) Type-level options (most specific)
        foreach (var ad in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(ad.AttributeClass, ks.GenerateNonTryOptionsAttribute))
                return AttributeConverters.ToOptions(ad);
        }

        // 2) Assembly-level options
        foreach (var ad in type.ContainingAssembly.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(ad.AttributeClass, ks.GenerateNonTryOptionsAttribute))
                return AttributeConverters.ToOptions(ad);
        }

        // 3) Defaults
        return new GenerateNonTryOptionsAttributeInfo(Location.None);
    }

    private static ITypeSymbol ResolveExceptionType(
        SourceProductionContext spc,
        KnownSymbols ks,
        GenerateNonTryMethodAttributeInfo attr)
    {
        // If explicitly provided and derives from System.Exception → use it
        if (attr.ExceptionType is INamedTypeSymbol provided &&
            provided.IsDerivedFrom(ks.ExceptionBase))
        {
            return provided;
        }

        // If explicitly provided but invalid → warn and fall back to InvalidOperationException
        if (attr.ExceptionType is INamedTypeSymbol badProvided)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.InvalidExceptionType,
                attr.Location,
                badProvided.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));

            return ks.InvalidOperationException;
        }

        // No type provided → use InvalidOperationException
        return ks.InvalidOperationException;
    }


    /// <summary>
    /// Builds either the partial/interface container or an extensions class,
    /// and fills it with the appropriate methods.
    /// </summary>
    private static MemberDeclarationSyntax BuildTypeContainer(
        TypeSpec spec,
        TypeEmissionPlan plan,
        ImmutableArray<PlannedMethod> planned)
    {
        // Split methods by emission kind
        var partials = planned.Where(p =>
            p.Signature.Kind == EmissionKind.Partial ||
            p.Signature.Kind == EmissionKind.InterfaceDefault).ToArray();

        var extensions = planned.Where(p =>
            p.Signature.Kind == EmissionKind.Extension).ToArray();

        // 1) Base type container (same type as source) populated with partial/interface methods.
        var baseTypeDecl = BuildTargetTypeDeclaration(spec.Type, partials);

        // 2) Optional extensions static class in the same namespace.
        if (extensions.Length == 0)
            return baseTypeDecl;

        var extDecl = BuildExtensionsContainer(spec.Type, extensions);

        // If we have only extensions (no in-place members), return the extensions container.
        if (partials.Length == 0)
            return extDecl;

        // If we have both, return the base container here; emit extDecl in a separate file from the driver.
        return baseTypeDecl;
    }

    /// <summary>Creates the target type declaration and inserts methods (partial or interface default).</summary>
    private static MemberDeclarationSyntax BuildTargetTypeDeclaration(
        INamedTypeSymbol type,
        IReadOnlyList<PlannedMethod> methods)
    {
        // 1) Create a TypeDeclarationSyntax matching kind/name/modifiers (partial assumed).
        var identifier = Identifier(type.Name);
        var modifiers = TokenList(Token(SyntaxKind.PartialKeyword)); // you can prepend public/internal/etc. as desired

        TypeDeclarationSyntax decl =
            type.TypeKind switch
            {
                TypeKind.Interface => InterfaceDeclaration(identifier).WithModifiers(modifiers),
                TypeKind.Structure => StructDeclaration(identifier).WithModifiers(modifiers),
                _ => ClassDeclaration(identifier).WithModifiers(modifiers)
            };

        // 2) Add type parameters + constraints (skipped here for brevity).
        // 3) Add method members.
        var members = methods.Select(BuildMethodDeclaration);
        return decl.WithMembers(List<MemberDeclarationSyntax>(members));
    }

    /// <summary>Creates the static extensions class and inserts extension methods.</summary>
    private static MemberDeclarationSyntax BuildExtensionsContainer(
        INamedTypeSymbol type,
        IReadOnlyList<PlannedMethod> methods)
    {
        var extName = Identifier($"{type.Name}_NonTryExtensions");
        var decl = ClassDeclaration(extName)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)));

        var members = methods.Select(BuildMethodDeclaration);
        return decl.WithMembers(List<MemberDeclarationSyntax>(members));
    }

    /// <summary>
    /// Build a single method declaration (signature + body) from the plan.
    /// Handles sync and async, partial/iface/extension uniformly.
    /// </summary>
    private static MethodDeclarationSyntax BuildMethodDeclaration(PlannedMethod pm)
        => pm.IsAsync ? BuildNonTryAsync(pm) : BuildNonTrySync(pm);

    // Maps (kind, sourceIsStatic) -> (isPublic, isStatic, includePublicModifier)
    private static (bool isPublic, bool isStatic) GetModifiers(PlannedMethod pm) => pm.Signature.Kind switch
    {
        EmissionKind.Partial => (true, pm.Signature.IsStatic), // mirror static, public
        EmissionKind.Extension => (true, true), // public static
        EmissionKind.InterfaceDefault => (false, false), // no 'public', instance
        _ => throw new InvalidOperationException($"Unsupported kind {pm.Signature.Kind}")
    };

    private static MethodDeclarationSyntax CreateShell(PlannedMethod pm, bool isAsync)
    {
        var (isPublic, isStatic) = GetModifiers(pm);

        var ret = ParseTypeName(pm.Signature.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        var decl = MethodDeclaration(ret, pm.Signature.Name);

        var mods = new List<SyntaxToken>();
        if (isPublic) mods.Add(Token(SyntaxKind.PublicKeyword));
        if (isStatic) mods.Add(Token(SyntaxKind.StaticKeyword));
        if (isAsync) mods.Add(Token(SyntaxKind.AsyncKeyword));
        decl = decl.WithModifiers(TokenList(mods));

        var parameters = pm.Signature.Parameters.Select(RenderParameter);
        return decl.WithParameterList(ParameterList(SeparatedList(parameters)));
    }

    private static ExpressionSyntax BuildTryInvocation(PlannedMethod pm, bool isAsync, string? outVarName = null)
    {
        // If you later want reduced extension calls (self.TryFoo(...)), adjust here.
        // For now we just invoke by name with args rendered below.
        return InvocationExpression(IdentifierName(pm.Source.Method.Name))
            .WithArgumentList(ArgumentList(SeparatedList(
                isAsync
                    ? RenderTryArgsAsync(pm)
                    : RenderTryArgsSync(pm, outVarName!)
            )));
    }

    private static ThrowStatementSyntax BuildThrow(PlannedMethod pm) =>
        ThrowStatement(ObjectCreationExpression(
                ParseTypeName(pm.ExceptionType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))
            .WithArgumentList(ArgumentList()));

    private static MethodDeclarationSyntax BuildNonTrySync(PlannedMethod pm)
    {
        // PSEUDOCODE:
        // [public] [static] T Foo(...) { if (TryFoo(..., out myOut)) return myOut; throw new Ex(); }
        var m = CreateShell(pm, isAsync: false);

        // Identify the out parameter in the source method
        var outParam = pm.Source.Method.Parameters.First(p => p.RefKind == RefKind.Out).Name;

        // Invocation uses all original parameter names, with out <outParam>
        var tryCall = InvocationExpression(IdentifierName(pm.Source.Method.Name))
            .WithArgumentList(ArgumentList(SeparatedList(RenderTryArgsSync(pm, outParam))));

        var body = Block(
            IfStatement(tryCall, Block(ReturnStatement(IdentifierName(outParam)))),
            BuildThrow(pm)
        );

        return m.WithBody(body);
    }

    private static MethodDeclarationSyntax BuildNonTryAsync(PlannedMethod pm)
    {
        // PSEUDOCODE:
        // [public] [static] async Task<T> FooAsync(...) { var t = await TryFooAsync(...); if (t.Item1) return t.Item2; throw new Ex(); }
        var m = CreateShell(pm, isAsync: true);

        var tmp = Identifier("tmp");
        var awaitCall = AwaitExpression(BuildTryInvocation(pm, isAsync: true));

        var decl = LocalDeclarationStatement(
            VariableDeclaration(IdentifierName("var"))
                .WithVariables(SeparatedList(new[]
                {
                    VariableDeclarator(tmp).WithInitializer(EqualsValueClause(awaitCall))
                })));

        var ok = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(tmp),
            IdentifierName("Item1"));
        var value = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(tmp),
            IdentifierName("Item2"));

        var body = Block(
            decl,
            IfStatement(ok, Block(ReturnStatement(value))),
            BuildThrow(pm)
        );

        return m.WithBody(body);
    }

    private static IEnumerable<ArgumentSyntax> RenderTryArgsSync(PlannedMethod pm, string outParamName)
    {
        foreach (var p in pm.Source.Method.Parameters)
        {
            if (p.RefKind == RefKind.Out)
            {
                yield return Argument(
                        DeclarationExpression(IdentifierName("var"), SingleVariableDesignation(Identifier(outParamName))))
                    .WithRefKindKeyword(Token(SyntaxKind.OutKeyword));
            }
            else
            {
                var arg = Argument(IdentifierName(p.Name));
                if (p.RefKind == RefKind.Ref) arg = arg.WithRefKindKeyword(Token(SyntaxKind.RefKeyword));
                if (p.RefKind == RefKind.In)  arg = arg.WithRefKindKeyword(Token(SyntaxKind.InKeyword));
                yield return arg;
            }
        }
    }

    private static IEnumerable<ArgumentSyntax> RenderTryArgsAsync(PlannedMethod pm)
    {
        foreach (var p in pm.Source.Method.Parameters)
        {
            var arg = Argument(IdentifierName(p.Name));
            if (p.RefKind == RefKind.Ref) arg = arg.WithRefKindKeyword(Token(SyntaxKind.RefKeyword));
            if (p.RefKind == RefKind.In) arg = arg.WithRefKindKeyword(Token(SyntaxKind.InKeyword));
            yield return arg;
        }
    }

    private static ParameterSyntax RenderParameter(IParameterSymbol p)
    {
        // this/ref/out/in
        var mods = new List<SyntaxToken>();
        if (p.IsThis) mods.Add(Token(SyntaxKind.ThisKeyword));
        switch (p.RefKind)
        {
            case RefKind.Ref: mods.Add(Token(SyntaxKind.RefKeyword)); break;
            case RefKind.Out: mods.Add(Token(SyntaxKind.OutKeyword)); break;
            case RefKind.In: mods.Add(Token(SyntaxKind.InKeyword)); break;
        }

        var typeSyntax = ParseTypeName(p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        var param = Parameter(Identifier(p.Name)).WithType(typeSyntax);

        if (mods.Count > 0) param = param.WithModifiers(TokenList(mods));

        if (p.IsParams)
            param = param.WithModifiers(TokenList(param.Modifiers.Concat(new[] { Token(SyntaxKind.ParamsKeyword) })));

        if (p.HasExplicitDefaultValue)
        {
            var equals = EqualsValueClause(GenerationHelpers.ToCSharpLiteralExpression(p.ExplicitDefaultValue));
            param = param.WithDefault(equals);
        }

        return param;
    }
}