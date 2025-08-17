using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        context.RegisterPostInitializationOutput(pi =>
        {
            pi.AddSource("NonTryMethods.__smoketest.g.cs",
                "// If you can see this, the package/analyzer wiring is OK.");
        });

        // Known types by reference
        IncrementalValueProvider<KnownSymbols?> knownSymbolsProvider =
            context.CompilationProvider.Select((comp, _) => KnownSymbols.TryCreateInstance(comp));

        context.RegisterSourceOutput(knownSymbolsProvider, (productionContext, symbols) =>
        {
            productionContext.AddSource("NonTryMethods.__symbols.g.cs",
                "// attrib " + symbols?.GenerateNonTryMethodAttribute);
        });

        // NonTry Attributes that are invalid
        IncrementalValuesProvider<(bool invalid, string pattern, Location loc)> invalidAttributes =
            context.SyntaxProvider.ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: KnownSymbols.NonTryAttribute,
                    predicate: static (_, _) => true, // already filtered by name; keep cheap
                    transform: static (ctx, ct) =>
                    {
                        // There can be multiple matching attributes on the same target.
                        var results = new List<(AttributeData attr, Location loc)>(ctx.Attributes.Length);
                        foreach (var a in ctx.Attributes)
                        {
                            var loc = a.ApplicationSyntaxReference?.GetSyntax(ct)?.GetLocation()
                                      ?? ctx.TargetSymbol.Locations.FirstOrDefault()
                                      ?? Location.None;
                            results.Add((a, loc));
                        }

                        return results.ToImmutableArray();
                    })
                .SelectMany(static (arr, _) => arr)
                .Select(static (x, _) =>
                {
                    var model = AttributeConverters.ToNonTry(in x.attr);
                    var pattern = model.MethodNamePattern;

                    bool invalid = string.IsNullOrWhiteSpace(pattern)
                                   || !AttributeValidation.IsValidRegexPattern(pattern!);

                    return (invalid, pattern, x.loc);
                })
                .Where(static t => t.invalid);

        context.RegisterSourceOutput(invalidAttributes, static (spc, diag) =>
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.RegularExpressionIsInvalid,
                diag.loc,
                diag.pattern ?? "<null>"));
        });

        // All classes+interfaces
        IncrementalValuesProvider<INamedTypeSymbol> typesProvider = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax asType &&
                                    asType.Kind() is SyntaxKind.ClassDeclaration or SyntaxKind.InterfaceDeclaration
                                        or SyntaxKind.StructDeclaration,
                static (ctx, _) => (INamedTypeSymbol?)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node))
            .Where(static t => t is not null)
            .Select(static (s, _) => s!);

        // Assembly-level attributes
        IncrementalValueProvider<ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern>>
            assemblyRuleProvider =
                knownSymbolsProvider.Combine(context.CompilationProvider)
                    .Select((t, _) => AttributesCollection.From(t.Left, t.Right.Assembly));

        IncrementalValuesProvider<TypeSpec> perType = typesProvider.Combine(knownSymbolsProvider)
            .Combine(assemblyRuleProvider)
            .Where(static tuple =>
            {
                KnownSymbols? knownSymbols = tuple.Left.Right;
                INamedTypeSymbol typeSymbol = tuple.Left.Left;
                ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> assemblyAttributes = tuple.Right;

                if (knownSymbols == null)
                    return false;

                // If assembly has attribute => include
                if (assemblyAttributes.Length > 0)
                    return true;

                // If type has attribute => include
                if (typeSymbol.GetAttributes().Any(a =>
                        a.AttributeClass?.Equals(knownSymbols.GenerateNonTryMethodAttribute,
                            SymbolEqualityComparer.Default) ?? false))
                    return true;

                return false;
            })
            .Select(static (tuple, _) =>
            {
                KnownSymbols knownSymbols = tuple.Left.Right!;
                INamedTypeSymbol typeSymbol = tuple.Left.Left;
                ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> assemblyAttributes = tuple.Right;
                ImmutableArray<GenerateNonTryMethodAttributeInfoWithValidPattern> classAttributes =
                    AttributesCollection.From(knownSymbols, typeSymbol);

                List<MethodSpec>? res = null;
                foreach (IMethodSymbol? method in typeSymbol.GetMembers().OfType<IMethodSymbol>())
                {
                    if (method.Name.Length == 0)
                        continue;

                    if (classAttributes.Any(a => a.Pattern.IsMatch(method.Name)))
                    {
                        res ??= [];
                        res.Add(new MethodSpec(method, classAttributes));
                        continue;
                    }

                    if (assemblyAttributes.Any(a => a.Pattern.IsMatch(method.Name)))
                    {
                        res ??= [];
                        res.Add(new MethodSpec(method, assemblyAttributes));
                    }
                }

                return res is null ? null : new TypeSpec(knownSymbols, typeSymbol, [..res]);
            })
            .Where(static s => s != null)
            .Select(static (s, _) => s!);

        // We also need the Compilation to compute minimal type names with the right using-context
        context.RegisterSourceOutput(perType.Combine(context.CompilationProvider), static (spc, tuple) =>
        {
            var typeSpec = tuple.Left;
            var compilation = tuple.Right;

            var plan = DetermineTypeStrategy(typeSpec);
            var planned = PlanAllMethods(spc, typeSpec, plan);
            var filtered = FilterCollisionsAndDuplicates(spc, typeSpec, planned);

            if (filtered.Length == 0)
                return;

            // Now build the *real* CU using the semanticModel + position
            var minimalInfo = GenerationHelpers.GetSourceDisplayContext(compilation, typeSpec.Type);

            var cuReal = BuildCompilationUnit(typeSpec, filtered, minimalInfo,
                needsTasks: filtered.Any(pm => pm.IsAsync));

            spc.AddSource(GetHintName(typeSpec.Type),
                SourceText.From(cuReal.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
        });
    }

    // ---------------------------------------------------------------
    //  Planning & filtering (unchanged behavior, compacted where safe)
    // ---------------------------------------------------------------

    private static IEnumerable<PlannedMethod> PlanAllMethods(SourceProductionContext spc, TypeSpec spec,
        TypeEmissionPlan plan)
    {
        foreach (var m in spec.Methods)
            if (TryPlanMethod(spc, spec, plan, m, out var planned))
                yield return planned;
    }

    private static string GetHintName(INamedTypeSymbol type)
    {
        // A.B.Outer.Inner`1 -> A.B.Outer.Inner.NonTry.g.cs (strip arity on leaf)
        var simple = type.Name;
        var tick = simple.IndexOf('`');
        if (tick >= 0) simple = simple.Substring(0, tick);

        string ns = "";
        var nsSym = type.ContainingNamespace;
        if (nsSym is not null && !nsSym.IsGlobalNamespace)
        {
            ns = nsSym.ToDisplayString();
            if (ns.Length != 0) ns += ".";
        }

        return ns + simple + ".NonTry.g.cs";
    }

    private static CompilationUnitSyntax BuildCompilationUnit(TypeSpec spec,
        ImmutableArray<PlannedMethod> planned, in MinimalStringInfo minimalStringInfo, bool needsTasks)
    {
        var headerTrivia = TriviaList(
            Comment("// <auto-generated/>"),
            CarriageReturnLineFeed,
            Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), isActive: true)),
            CarriageReturnLineFeed
        );

        // Usings: System, optionally Tasks
        var usings = new List<UsingDirectiveSyntax> { UsingDirective(ParseName("System")) };
        if (needsTasks) usings.Add(UsingDirective(ParseName("System.Threading.Tasks")));

        var ns = spec.Type.ContainingNamespace;
        var container = BuildTypeContainer(spec, planned, minimalStringInfo);

        CompilationUnitSyntax cu = CompilationUnit().WithUsings(List(usings));
        if (ns is { IsGlobalNamespace: false })
        {
            var nsDecl = FileScopedNamespaceDeclaration(ParseName(ns.ToDisplayString()))
                .WithMembers(SingletonList(container));
            cu = cu.WithMembers(SingletonList<MemberDeclarationSyntax>(nsDecl));
        }
        else
        {
            cu = cu.WithMembers(SingletonList(container));
        }

        return cu.WithLeadingTrivia(headerTrivia);
    }

    private static TypeEmissionPlan DetermineTypeStrategy(TypeSpec spec)
    {
        var opts = GetEffectiveOptions(spec);
        var type = spec.Type;
        bool isInterface = type.TypeKind == TypeKind.Interface;
        bool supportsIfaceDefaults = true; // assume C# 8+

        var strategy = opts.MethodsGenerationStrategy == MethodsGenerationStrategy.Auto
            ? MethodsGenerationStrategy.PartialType
            : opts.MethodsGenerationStrategy;

        return new TypeEmissionPlan(strategy, isInterface: isInterface);
    }

    private static MethodClassification ClassifyMethodShape(IMethodSymbol method,
        GenerateNonTryOptionsAttributeInfo opts, KnownSymbols ks)
    {
        // sync bool + out T
        if (method.ReturnType.SpecialType == SpecialType.System_Boolean)
        {
            IParameterSymbol? outParam = null;
            foreach (var p in method.Parameters)
            {
                if (p.RefKind == RefKind.Out)
                {
                    if (outParam != null)
                        return new MethodClassification(MethodShape.NotEligible, null, isValueTask: false,
                            payloadType: null);
                    outParam = p;
                }
            }

            if (outParam != null)
                return new MethodClassification(MethodShape.SyncBoolOut, outParam, isValueTask: false,
                    payloadType: null);
            return new MethodClassification(MethodShape.NotEligible, null, isValueTask: false, payloadType: null);
        }

        // async Task<(bool,T)> / ValueTask<(bool,T)>
        if ((opts.AsyncCandidateStrategy & AsyncCandidateStrategy.TupleBooleanAndValue) != 0)
        {
            if (method.ReturnType is INamedTypeSymbol rt && rt.IsGenericType)
            {
                if (ks.TaskOfT is not null && SymbolEqualityComparer.Default.Equals(rt.OriginalDefinition, ks.TaskOfT))
                {
                    var tArg = rt.TypeArguments[0];
                    if (TryGetTupleBoolPayload(tArg, out var payload))
                        return new MethodClassification(MethodShape.AsyncTuple, null, isValueTask: false,
                            payloadType: payload);
                }

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

    private static bool TryPlanMethod(SourceProductionContext spc, TypeSpec typeSpec, TypeEmissionPlan plan,
        MethodSpec methodSpec, out PlannedMethod planned)
    {
        planned = default;
        var method = methodSpec.Method;
        var ks = typeSpec.Symbols;

        // 1) pick attribute (report NT007 MultiplePatternsMatchMethod if >1)
        GenerateNonTryMethodAttributeInfoWithValidPattern attrib;
        {
            var matches = methodSpec.ApplicableAttributes.Where(a => a.Pattern.IsMatch(method.Name)).ToArray();
            if (matches.Length == 0)
                throw new InvalidOperationException("Expected method to have 1+ matching attributes");
            if (matches.Length >= 2)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MultiplePatternsMatchMethod,
                    method.Locations.FirstOrDefault(),
                    method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), method.Name,
                    typeSpec.Type.Name,
                    string.Join(", ", matches.Select(s => $"'{s.Pattern}'"))));
            }

            attrib = matches[0];
        }

        var opts = GetEffectiveOptions(typeSpec);
        var cls = ClassifyMethodShape(method, opts, ks);
        if (cls.Shape == MethodShape.NotEligible)
        {
            var looksSync = method.ReturnType.SpecialType == SpecialType.System_Boolean ||
                            method.Parameters.Any(p => p.RefKind == RefKind.Out);
            spc.ReportDiagnostic(Diagnostic.Create(
                looksSync ? Diagnostics.NotEligibleSync : Diagnostics.NotEligibleAsyncShape,
                method.Locations.FirstOrDefault(),
                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), attrib.MethodNamePattern,
                opts.AsyncCandidateStrategy));
            return false;
        }

        string newName = ComputeGeneratedName(method.Name, attrib);
        var exType = ResolveExceptionType(spc, ks, attrib);

        EmissionKind kind = plan.Strategy == MethodsGenerationStrategy.Extensions ? EmissionKind.Extension
            : plan.IsInterface ? EmissionKind.InterfaceDefault
            : EmissionKind.Partial;

        if (kind == EmissionKind.Extension && method.IsStatic)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.UnableToGenerateExtensionMethodForStaticMethod,
                method.Locations.FirstOrDefault(),
                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), method.Name, typeSpec.Type.Name));
            return false;
        }

        bool isAsync = cls.Shape == MethodShape.AsyncTuple;
        bool isValueTask = cls.IsValueTask;

        ITypeSymbol returnType;
        ImmutableArray<IParameterSymbol> parameters;

        if (!isAsync)
        {
            var outP = cls.OutParam!;
            returnType = AdjustForReturnStrategy(outP.Type, opts.ReturnGenerationStrategy);
            parameters = [..method.Parameters.Where(p => !SymbolEqualityComparer.Default.Equals(p, outP))];
        }
        else
        {
            var payload = AdjustForReturnStrategy(cls.PayloadType!, opts.ReturnGenerationStrategy);
            INamedTypeSymbol wrapper = isValueTask ? ks.ValueTaskOfT : ks.TaskOfT;
            returnType = wrapper.Construct(payload);
            parameters = [..method.Parameters];
        }

        bool genIsStatic = kind switch
        {
            EmissionKind.Partial => method.IsStatic,
            EmissionKind.InterfaceDefault => false,
            EmissionKind.Extension => true,
            _ => method.IsStatic
        };

        var sig = new PlannedSignature(kind, newName, returnType, parameters, genIsStatic);
        planned = new PlannedMethod(methodSpec, sig, exType, isAsync);
        return true;

        static string ComputeGeneratedName(string original, GenerateNonTryMethodAttributeInfoWithValidPattern info)
        {
            var m = info.Pattern.Match(original);
            Debug.Assert(m.Success);

            return m.Groups[1].Value;
        }

        static ITypeSymbol AdjustForReturnStrategy(ITypeSymbol t, ReturnGenerationStrategy strat)
        {
            if (strat == ReturnGenerationStrategy.Verbatim) return t;
            if (t is INamedTypeSymbol nt && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                return nt.TypeArguments[0];
            if (t.NullableAnnotation == NullableAnnotation.Annotated && (t.IsReferenceType || t.IsAnonymousType))
                return t.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            return t;
        }
    }

    private static ImmutableArray<PlannedMethod> FilterCollisionsAndDuplicates(SourceProductionContext spc,
        TypeSpec typeSpec, IEnumerable<PlannedMethod> candidates)
    {
        var seen = new Dictionary<SignatureKey, PlannedMethod>();
        var deduped = new List<PlannedMethod>();
        foreach (var pm in candidates)
        {
            var key = SignatureKey.From(pm);
            if (seen.ContainsKey(key))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.DuplicateGeneratedSignature,
                    pm.Source.Method.Locations.FirstOrDefault(),
                    pm.Signature.Name,
                    typeSpec.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                continue;
            }

            seen[key] = pm;
            deduped.Add(pm);
        }

        if (deduped.Count == 0) return ImmutableArray<PlannedMethod>.Empty;

        if (deduped.Any(p => p.Signature.Kind != EmissionKind.Extension))
        {
            var existing = new HashSet<SignatureKey>();
            foreach (var m in typeSpec.Type.GetMembers().OfType<IMethodSymbol>())
                if (m.MethodKind == MethodKind.Ordinary)
                    existing.Add(SignatureKey.FromExisting(m));

            var filtered = new List<PlannedMethod>(deduped.Count);
            foreach (var pm in deduped)
            {
                if (pm.Signature.Kind == EmissionKind.Extension)
                {
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
                    continue;
                }

                filtered.Add(pm);
            }

            return [..filtered];
        }

        return [..deduped];
    }

    private static GenerateNonTryOptionsAttributeInfo GetEffectiveOptions(TypeSpec spec)
    {
        var ks = spec.Symbols;
        var type = spec.Type;
        foreach (var ad in type.GetAttributes())
            if (SymbolEqualityComparer.Default.Equals(ad.AttributeClass, ks.GenerateNonTryOptionsAttribute))
                return AttributeConverters.ToOptions(ad);
        foreach (var ad in type.ContainingAssembly.GetAttributes())
            if (SymbolEqualityComparer.Default.Equals(ad.AttributeClass, ks.GenerateNonTryOptionsAttribute))
                return AttributeConverters.ToOptions(ad);
        return new GenerateNonTryOptionsAttributeInfo(Location.None);
    }

    private static ITypeSymbol ResolveExceptionType(SourceProductionContext spc, KnownSymbols ks,
        GenerateNonTryMethodAttributeInfoWithValidPattern attr)
    {
        if (attr.ExceptionType is INamedTypeSymbol provided and { } && provided.IsDerivedFrom(ks.ExceptionBase))
            return provided;
        if (attr.ExceptionType is INamedTypeSymbol bad)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.InvalidExceptionType,
                attr.Location,
                bad.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return ks.InvalidOperationException;
        }

        return ks.InvalidOperationException;
    }

    private static MethodDeclarationSyntax BuildMethodDeclaration(PlannedMethod pm,
        in MinimalStringInfo minimalStringInfo)
        => pm.IsAsync ? BuildNonTryAsync(pm, minimalStringInfo) : BuildNonTrySync(pm, minimalStringInfo);

    private static MethodDeclarationSyntax CreateShell(PlannedMethod pm, bool isAsync,
        in MinimalStringInfo minimalStringInfo)
    {
        var (visibilityKind, isStatic) = GetModifiers(pm);
        var ret = ParseTypeName(pm.Signature.ReturnType.ToMinimalDisplayString(minimalStringInfo));
        var decl = MethodDeclaration(ret, pm.Signature.Name);

        var mods = new List<SyntaxToken>();
        if (visibilityKind.HasValue)
            mods.Add(Token(visibilityKind.Value));
        if (isStatic)
            mods.Add(Token(SyntaxKind.StaticKeyword));
        if (isAsync)
            mods.Add(Token(SyntaxKind.AsyncKeyword));
        decl = decl.WithModifiers(TokenList(mods));

        // Add XML docs
        var crefName = pm.Source.Method.ToMinimalDisplayString(minimalStringInfo, Display.CrefFormat);
        var lines = new[]
        {
            "/// <summary>",
            $"/// This is a non-try variant of <see cref=\"{crefName}\"/>.",
            "/// </summary>",
            "/// <remarks>This was auto-generated.</remarks>",
        };
        var trivia = TriviaList(lines.Select(Comment));
        decl = decl.WithLeadingTrivia(trivia.Add(ElasticCarriageReturnLineFeed));

        return decl;
    }

    private static (SyntaxKind? visibilityKind, bool isStatic) GetModifiers(PlannedMethod pm)
    {
        var src = pm.Source.Method;

        // Interface members: omit visibility keyword so C# defaults to 'public'
        if (src.ContainingType?.TypeKind == TypeKind.Interface)
        {
            if (pm.Signature.Kind == EmissionKind.Extension)
            {
                // If we're emitting extensions, we must mark these public
                return (SyntaxKind.PublicKeyword, GetIsStatic(pm));
            }

            return (null, GetIsStatic(pm));
        }

        SyntaxKind? visibility = src.DeclaredAccessibility switch
        {
            Accessibility.Public => SyntaxKind.PublicKeyword,
            Accessibility.Internal => SyntaxKind.InternalKeyword,
            Accessibility.Private => SyntaxKind.PrivateKeyword,
            Accessibility.Protected => SyntaxKind.ProtectedKeyword,

            // Compound accessibilities need two tokens; return null here.
            // If you need them later, change the return type to SyntaxTokenList.
            Accessibility.ProtectedOrInternal => null, // would be 'protected internal'
            Accessibility.ProtectedAndInternal => null, // would be 'private protected'

            _ => null
        };

        return (visibility, GetIsStatic(pm));

        static bool GetIsStatic(PlannedMethod pm) => pm.Signature.Kind switch
        {
            EmissionKind.Partial => pm.Signature.IsStatic,
            EmissionKind.Extension => true,
            EmissionKind.InterfaceDefault => false,
            _ => pm.Signature.IsStatic
        };
    }

    private static bool NeedsInReceiver(IMethodSymbol m)
        => m.ContainingType.IsValueType && !m.IsStatic && m.IsReadOnly;

    private static bool NeedsRefReceiver(IMethodSymbol m)
        => m.ContainingType.IsValueType && !m.IsStatic && !m.IsReadOnly;

    private static ParameterSyntax BuildExtensionThisParam(PlannedMethod pm, string receiverName,
        in MinimalStringInfo minimalStringInfo)
    {
        var m = pm.Source.Method;
        var recvType = ParseTypeName(m.ContainingType.ToMinimalDisplayString(minimalStringInfo));
        var mods = new List<SyntaxToken> { Token(SyntaxKind.ThisKeyword) };
        if (NeedsRefReceiver(m)) mods.Add(Token(SyntaxKind.RefKeyword));
        else if (NeedsInReceiver(m)) mods.Add(Token(SyntaxKind.InKeyword));
        return Parameter(Identifier(receiverName)).WithType(recvType).WithModifiers(TokenList(mods));
    }

    // Returns params list and call target expression (method identifier or receiver.member)
    private static (SeparatedSyntaxList<ParameterSyntax> @params, ExpressionSyntax target)
        ComputeExtensionBits(PlannedMethod pm, in MinimalStringInfo minimalStringInfo)
    {
        var info = minimalStringInfo;
        var renderedParams = pm.Signature.Parameters.Select(p => RenderParameter(p, info));
        if (pm.Signature.Kind != EmissionKind.Extension)
            return (SeparatedList(renderedParams), IdentifierName(pm.Source.Method.Name));

        string recv = GenerationHelpers.FindUnusedParamName(pm.Signature.Parameters, "self");
        var thisParam = BuildExtensionThisParam(pm, recv, minimalStringInfo);
        var allParams = new[] { thisParam }.Concat(renderedParams);
        var target = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(recv),
            IdentifierName(pm.Source.Method.Name));
        return (SeparatedList(allParams), target);
    }

    private static IEnumerable<ArgumentSyntax> RenderTryArgs(PlannedMethod pm, bool isAsync)
    {
        foreach (var p in pm.Source.Method.Parameters)
        {
            if (!isAsync && p.RefKind == RefKind.Out)
            {
                // Sync pattern: out var <originalName>
                yield return Argument(
                        DeclarationExpression(
                            IdentifierName("var"),
                            SingleVariableDesignation(Identifier(p.Name))))
                    .WithRefKindKeyword(Token(SyntaxKind.OutKeyword));
                continue;
            }

            var arg = Argument(IdentifierName(p.Name));
            if (p.RefKind == RefKind.Ref) arg = arg.WithRefKindKeyword(Token(SyntaxKind.RefKeyword));
            if (p.RefKind == RefKind.In) arg = arg.WithRefKindKeyword(Token(SyntaxKind.InKeyword));
            yield return arg;
        }
    }

    private static ThrowStatementSyntax BuildThrow(PlannedMethod pm, in MinimalStringInfo minimalStringInfo)
        => ThrowStatement(
            ObjectCreationExpression(
                    ParseTypeName(pm.ExceptionType.ToMinimalDisplayString(minimalStringInfo)))
                .WithArgumentList(ArgumentList()));

    // [public] [static] T Foo([this C self,] ...) { if (target.TryFoo(..., out outParam)) return outParam; throw new Ex(); }
    private static MethodDeclarationSyntax BuildNonTrySync(PlannedMethod pm, in MinimalStringInfo minimalStringInfo)
    {
        var decl = CreateShell(pm, isAsync: false, minimalStringInfo);
        var (pars, target) = ComputeExtensionBits(pm, minimalStringInfo);
        decl = decl.WithParameterList(ParameterList(pars));

        var outParam = pm.Source.Method.Parameters.First(p => p.RefKind == RefKind.Out).Name;
        var tryCall = InvocationExpression(target)
            .WithArgumentList(ArgumentList(SeparatedList(RenderTryArgs(pm, isAsync: false))));

        return decl.WithBody(Block(
            IfStatement(tryCall, Block(ReturnStatement(IdentifierName(outParam)))),
            BuildThrow(pm, minimalStringInfo)
        ));
    }

    // [public] [static] async Task<T> FooAsync([this C self,] ...) { var t = await target.TryFooAsync(...); if (t.Item1) return t.Item2; throw new Ex(); }
    private static MethodDeclarationSyntax BuildNonTryAsync(PlannedMethod pm, in MinimalStringInfo minimalStringInfo)
    {
        var decl = CreateShell(pm, isAsync: true, minimalStringInfo);
        var (pars, target) = ComputeExtensionBits(pm, minimalStringInfo);
        decl = decl.WithParameterList(ParameterList(pars));

        var awaitCall = AwaitExpression(InvocationExpression(target)
            .WithArgumentList(ArgumentList(SeparatedList(RenderTryArgs(pm, isAsync: true)))));

        var varName = GenerationHelpers.FindUnusedParamName(pm.Signature.Parameters, "tmp");

        var t = Identifier(varName);
        var declLocal = LocalDeclarationStatement(
            VariableDeclaration(IdentifierName("var"))
                .WithVariables(SeparatedList([VariableDeclarator(t).WithInitializer(EqualsValueClause(awaitCall))])));

        var ok = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(t),
            IdentifierName("Item1"));
        var val = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(t),
            IdentifierName("Item2"));

        return decl.WithBody(Block(
            declLocal,
            IfStatement(ok, Block(ReturnStatement(val))),
            BuildThrow(pm, minimalStringInfo)
        ));
    }

    private static ParameterSyntax RenderParameter(IParameterSymbol p, in MinimalStringInfo minimalStringInfo)
    {
        var mods = new List<SyntaxToken>();
        if (p.IsThis) mods.Add(Token(SyntaxKind.ThisKeyword));
        switch (p.RefKind)
        {
            case RefKind.Ref: mods.Add(Token(SyntaxKind.RefKeyword)); break;
            case RefKind.Out: mods.Add(Token(SyntaxKind.OutKeyword)); break;
            case RefKind.In: mods.Add(Token(SyntaxKind.InKeyword)); break;
        }

        var typeSyntax = ParseTypeName(p.Type.ToMinimalDisplayString(minimalStringInfo));
        var param = Parameter(Identifier(p.Name)).WithType(typeSyntax);

        if (mods.Count > 0) param = param.WithModifiers(TokenList(mods));
        if (p.IsParams)
            param = param.WithModifiers(TokenList(param.Modifiers.Concat([Token(SyntaxKind.ParamsKeyword)])));
        if (p.HasExplicitDefaultValue)
        {
            var equals = EqualsValueClause(GenerationHelpers.ToCSharpLiteralExpression(p.ExplicitDefaultValue));
            param = param.WithDefault(equals);
        }

        return param;
    }

    private static MemberDeclarationSyntax BuildTypeContainer(TypeSpec spec,
        ImmutableArray<PlannedMethod> planned, in MinimalStringInfo minimalStringInfo)
    {
        var partials = planned.Where(p => p.Signature.Kind is EmissionKind.Partial or EmissionKind.InterfaceDefault)
            .ToArray();
        var extensions = planned.Where(p => p.Signature.Kind == EmissionKind.Extension).ToArray();

        var baseTypeDecl = BuildTargetTypeDeclaration(spec.Type, partials, minimalStringInfo);
        if (extensions.Length == 0) return baseTypeDecl;

        var extDecl = BuildExtensionsContainer(spec.Type, extensions, minimalStringInfo);
        if (partials.Length == 0) return extDecl;
        return baseTypeDecl; // driver emits extDecl in separate hint if desired
    }

    private static MemberDeclarationSyntax BuildTargetTypeDeclaration(INamedTypeSymbol type,
        IReadOnlyList<PlannedMethod> methods, in MinimalStringInfo minimalStringInfo)
    {
        var identifier = Identifier(type.Name);
        var modifiers = TokenList(Token(SyntaxKind.PartialKeyword));
        TypeDeclarationSyntax decl = type.TypeKind switch
        {
            TypeKind.Interface => InterfaceDeclaration(identifier).WithModifiers(modifiers),
            TypeKind.Structure => StructDeclaration(identifier).WithModifiers(modifiers),
            _ => ClassDeclaration(identifier).WithModifiers(modifiers)
        };

        var info = minimalStringInfo;
        var members = methods.Select(pm => BuildMethodDeclaration(pm, info));
        return decl.WithMembers(List<MemberDeclarationSyntax>(members));
    }

    private static MemberDeclarationSyntax BuildExtensionsContainer(INamedTypeSymbol type,
        IReadOnlyList<PlannedMethod> methods, in MinimalStringInfo minimalStringInfo)
    {
        var extName = Identifier($"{type.Name}_NonTryExtensions");
        var decl = ClassDeclaration(extName)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)));
        var info = minimalStringInfo;
        var members = methods.Select(pm => BuildMethodDeclaration(pm, info));
        return decl.WithMembers(List<MemberDeclarationSyntax>(members));
    }
}