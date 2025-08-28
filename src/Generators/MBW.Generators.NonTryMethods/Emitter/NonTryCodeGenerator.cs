using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MBW.Generators.Common.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace MBW.Generators.NonTryMethods.Emitter;

internal static class NonTryCodeGenerator
{
    // PUBLIC: build full method (modifiers + generics + params + body).
    public static MethodDeclarationSyntax Emit(in NonTryModel model, in EmissionPlan planIn,
        Accessibility accessibility, bool isStaticInPartial)
    {
        // Compute safe names up front (receiver name may be auto-chosen)
        var (plan, names) = ComputeNames(model, planIn);

        var returnType =
            ParseTypeName(model.GeneratedReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

        var decl = MethodDeclaration(returnType, Identifier(model.GeneratedName))
            .WithModifiers(BuildModifiers(accessibility, plan, isStaticInPartial, model.Source.Shape))
            .WithParameterList(BuildParameterList(model.Parameters, plan, names))
            .WithBody(BuildBody(model, plan, names));

        // Method type parameters + constraints
        var (tpList, constraints) = BuildMethodGenerics(model, plan);
        if (tpList is not null) decl = decl.WithTypeParameterList(tpList);
        if (constraints.Count > 0) decl = decl.WithConstraintClauses(constraints);

        return decl;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Name computation (receiver, out var, async temp) using your helper
    private readonly record struct ChosenNames(string Receiver, string OutValue, string Temp);

    private static (EmissionPlan plan, ChosenNames names) ComputeNames(in NonTryModel m, in EmissionPlan planIn)
    {
        var used = new HashSet<string>(m.Parameters.Select(p => p.Name), StringComparer.Ordinal);

        // Receiver for extension methods
        string receiver = planIn.Kind == EmissionKind.Extension
            ? (!string.IsNullOrWhiteSpace(planIn.SelfName)
                ? planIn.SelfName!
                : GenerationHelpers.FindUnusedParamName(used, "self"))
            : "this";

        // Reserve receiver if extension
        if (planIn.Kind == EmissionKind.Extension)
            used.Add(receiver);

        // Out variable name (avoid collisions with params/receiver)
        string outValue = GenerationHelpers.FindUnusedParamName(used, "value");
        used.Add(outValue);

        // Temp name for async tuple
        string temp = GenerationHelpers.FindUnusedParamName(used, "t");
        used.Add(temp);

        // Return adjusted plan if we auto-chose receiver
        var plan = planIn;
        if (planIn.Kind == EmissionKind.Extension && string.IsNullOrWhiteSpace(planIn.SelfName))
            plan = new EmissionPlan(planIn.Kind, receiver, planIn.ExtensionReceiverType, planIn.ReceiverRefKind);

        return (plan, new ChosenNames(receiver, outValue, temp));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Modifiers
    private static SyntaxTokenList BuildModifiers(Accessibility a, in EmissionPlan plan, bool isStaticInPartial,
        TryShape shape)
    {
        var toks = new List<SyntaxToken>();

        if (plan.Kind != EmissionKind.InterfaceDefault)
        {
            if (a == Accessibility.Public) toks.Add(Token(SyntaxKind.PublicKeyword));
            else if (a == Accessibility.Internal) toks.Add(Token(SyntaxKind.InternalKeyword));
        }

        if (plan.Kind == EmissionKind.Extension || (plan.Kind == EmissionKind.Partial && isStaticInPartial))
            toks.Add(Token(SyntaxKind.StaticKeyword));

        if (shape is TryShape.AsyncTupleTask or TryShape.AsyncTupleValueTask)
            toks.Add(Token(SyntaxKind.AsyncKeyword));

        return TokenList(toks);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Parameters (prepend `this` receiver for extensions; ref/out/in in Modifiers)
    private static ParameterListSyntax BuildParameterList(ImmutableArray<ParameterModel> ps, in EmissionPlan plan,
        in ChosenNames names)
    {
        var items = new List<ParameterSyntax>();

        if (plan.Kind == EmissionKind.Extension)
        {
            if (plan.ExtensionReceiverType is null)
                throw new InvalidOperationException("Extension emission requires a receiver type.");

            var recvType =
                ParseTypeName(plan.ExtensionReceiverType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            var recvMods = plan.ReceiverRefKind switch
            {
                RefKind.Ref => TokenList(Token(SyntaxKind.ThisKeyword), Token(SyntaxKind.RefKeyword)),
                RefKind.In => TokenList(Token(SyntaxKind.ThisKeyword), Token(SyntaxKind.InKeyword)),
                _ => TokenList(Token(SyntaxKind.ThisKeyword))
            };

            items.Add(
                Parameter(Identifier(names.Receiver))
                    .WithType(recvType)
                    .WithModifiers(recvMods));
        }

        foreach (var p in ps)
        {
            var type = ParseTypeName(p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            var mods =
                p.IsParams
                    ? TokenList(Token(SyntaxKind.ParamsKeyword))
                    : p.RefKind switch
                    {
                        RefKind.Ref => TokenList(Token(SyntaxKind.RefKeyword)),
                        RefKind.Out => TokenList(Token(SyntaxKind.OutKeyword)),
                        RefKind.In => TokenList(Token(SyntaxKind.InKeyword)),
                        _ => default
                    };

            var param = Parameter(Identifier(p.Name)).WithType(type).WithModifiers(mods);

            // Defaults only when not ref/out/params
            if (p.HasDefault && p.RefKind == RefKind.None && !p.IsParams)
                param = param.WithDefault(
                    EqualsValueClause(GenerationHelpers.ToCSharpLiteralExpression(p.DefaultValue)));

            items.Add(param);
        }

        return ParameterList(SeparatedList(items));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Method generics: type parameter list + where-clauses (incl. lifted receiver TPs)
    private static (TypeParameterListSyntax? tpList, SyntaxList<TypeParameterConstraintClauseSyntax> clauses)
        BuildMethodGenerics(in NonTryModel m, in EmissionPlan _)
    {
        var map = new Dictionary<string, ITypeParameterSymbol>(StringComparer.Ordinal);

        void add(ImmutableArray<ITypeParameterSymbol> src, bool prefer)
        {
            foreach (var tp in src)
            {
                if (prefer || !map.ContainsKey(tp.Name))
                    map[tp.Name] = tp;
            }
        }

        add(m.LiftedReceiverTypeParams, prefer: false);
        add(m.MethodTypeParams, prefer: true);

        if (map.Count == 0)
            return (null, default);

        var tpSyntax = SeparatedList(map.Keys.Select(n => TypeParameter(Identifier(n))));
        var tpList = TypeParameterList(tpSyntax);

        var clauseList = new List<TypeParameterConstraintClauseSyntax>(map.Count);
        foreach (var tp in map.Values)
        {
            var constraints = new List<TypeParameterConstraintSyntax>(8);

            if (tp.HasReferenceTypeConstraint)
            {
                var classC = ClassOrStructConstraint(SyntaxKind.ClassConstraint);
                if (tp.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
                    classC = classC.WithQuestionToken(Token(SyntaxKind.QuestionToken));
                constraints.Add(classC);
            }

            if (tp.HasValueTypeConstraint)
                constraints.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));

            if (tp.HasUnmanagedTypeConstraint)
                constraints.Add(TypeConstraint(ParseTypeName("unmanaged")));
            if (tp.HasNotNullConstraint)
                constraints.Add(TypeConstraint(ParseTypeName("notnull")));

            foreach (var ct in tp.ConstraintTypes)
                constraints.Add(
                    TypeConstraint(ParseTypeName(ct.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))));

            if (tp.HasConstructorConstraint)
                constraints.Add(ConstructorConstraint());

            if (constraints.Count > 0)
            {
                clauseList.Add(
                    TypeParameterConstraintClause(IdentifierName(tp.Name))
                        .WithConstraints(SeparatedList(constraints)));
            }
        }

        return (tpList, List(clauseList));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Body dispatcher
    private static BlockSyntax BuildBody(in NonTryModel m, in EmissionPlan plan, in ChosenNames names)
        => m.Source.Shape switch
        {
            TryShape.SyncOut => BuildBodySyncOut(m, plan, names),
            TryShape.AsyncTupleTask => BuildBodyAsyncTuple(m, plan, names),
            TryShape.AsyncTupleValueTask => BuildBodyAsyncTuple(m, plan, names),
            _ => Block()
        };

    // bool TryX(out T v, …) → if (Try(..., out var v)) return v[.Value]; throw …
    private static BlockSyntax BuildBodySyncOut(in NonTryModel m, in EmissionPlan plan, in ChosenNames names)
    {
        var rx = plan.Kind == EmissionKind.Extension
            ? (ExpressionSyntax)IdentifierName(names.Receiver)
            : ThisExpression();
        var callee = BuildCallee(m.Source, rx);

        var args = new List<ArgumentSyntax>(m.Parameters.Length + 1);
        foreach (var p in m.Parameters)
        {
            var a = Argument(IdentifierName(p.Name));
            if (p.RefKind == RefKind.Ref) a = a.WithRefKindKeyword(Token(SyntaxKind.RefKeyword));
            else if (p.RefKind == RefKind.Out) a = a.WithRefKindKeyword(Token(SyntaxKind.OutKeyword));
            else if (p.RefKind == RefKind.In) a = a.WithRefKindKeyword(Token(SyntaxKind.InKeyword));
            args.Add(a);
        }

        var insertAt = (m.Source.OutParamIndex is int idx && idx >= 0 && idx <= args.Count) ? idx : args.Count;
        var outVar = Argument(DeclarationExpression(IdentifierName("var"),
                SingleVariableDesignation(Identifier(names.OutValue))))
            .WithRefKindKeyword(Token(SyntaxKind.OutKeyword));
        args.Insert(insertAt, outVar);

        var invocation = InvocationExpression(ApplyTypeArgsIfAny(callee, m.Source))
            .WithArgumentList(ArgumentList(SeparatedList(args)));

        ExpressionSyntax valueExpr = IdentifierName(names.OutValue);
        if (m.Source.UnwrapNullableValue)
            valueExpr = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, valueExpr,
                IdentifierName("Value"));

        var then = ReturnStatement(valueExpr);

        return Block(IfStatement(invocation, then),
            ThrowStatement(NewException(m.ExceptionType)));
    }

    // await TryXAsync(…) (bool,T) → return T[.Value] or throw
    private static BlockSyntax BuildBodyAsyncTuple(in NonTryModel m, in EmissionPlan plan, in ChosenNames names)
    {
        var rx = plan.Kind == EmissionKind.Extension
            ? (ExpressionSyntax)IdentifierName(names.Receiver)
            : ThisExpression();
        var callee = BuildCallee(m.Source, rx);

        var aList = ArgumentList(SeparatedList(m.Parameters.Select(p =>
        {
            var a = Argument(IdentifierName(p.Name));
            if (p.RefKind == RefKind.Ref) a = a.WithRefKindKeyword(Token(SyntaxKind.RefKeyword));
            else if (p.RefKind == RefKind.Out) a = a.WithRefKindKeyword(Token(SyntaxKind.OutKeyword));
            else if (p.RefKind == RefKind.In) a = a.WithRefKindKeyword(Token(SyntaxKind.InKeyword));
            return a;
        })));

        var awaited =
            AwaitExpression(InvocationExpression(ApplyTypeArgsIfAny(callee, m.Source)).WithArgumentList(aList));

        var tDecl = LocalDeclarationStatement(
            VariableDeclaration(IdentifierName("var"))
                .WithVariables(SingletonSeparatedList(
                    VariableDeclarator(Identifier(names.Temp)).WithInitializer(EqualsValueClause(awaited)))));

        ExpressionSyntax valueExpr = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName(names.Temp), IdentifierName("Item2"));
        if (m.Source.UnwrapNullableValue)
            valueExpr = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, valueExpr,
                IdentifierName("Value"));

        var cond = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(names.Temp),
            IdentifierName("Item1"));
        var ret = ReturnStatement(valueExpr);
        var thr = ThrowStatement(NewException(m.ExceptionType));

        return Block(tDecl, IfStatement(cond, ret), thr);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Callee + type args
    private static ExpressionSyntax BuildCallee(in TrySourceDescriptor src, ExpressionSyntax receiverExpr)
    {
        if (!src.IsStatic)
            return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiverExpr,
                IdentifierName(src.Name));

        if (src.ContainingType is not null)
        {
            var left = ParseExpression(src.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left, IdentifierName(src.Name));
        }

        return IdentifierName(src.Name);
    }

    private static ExpressionSyntax ApplyTypeArgsIfAny(ExpressionSyntax callee, in TrySourceDescriptor src)
    {
        if (src.TypeArguments.IsDefaultOrEmpty) return callee;

        var gen = GenericName(Identifier(src.Name))
            .WithTypeArgumentList(TypeArgumentList(SeparatedList(src.TypeArguments.Select(ToTypeSyntax))));

        return callee switch
        {
            IdentifierNameSyntax => gen,
            MemberAccessExpressionSyntax ma => ma.WithName(gen),
            _ => callee
        };
    }

    // ──────────────────────────────────────────────────────────────────────
    // Utilities
    private static TypeSyntax ToTypeSyntax(ITypeSymbol s)
        => ParseTypeName(s.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

    private static ObjectCreationExpressionSyntax NewException(INamedTypeSymbol? ex)
    {
        var t = ex is null
            ? ParseTypeName("System.Exception")
            : ParseTypeName(ex.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        return ObjectCreationExpression(t).WithArgumentList(ArgumentList());
    }
}