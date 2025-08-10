using System.Linq;
using System.Text;
using MBW.Generators.NonTryMethods.Helpers;
using MBW.Generators.NonTryMethods.Objects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MBW.Generators.NonTryMethods;

[Generator]
public class AutogenNonTryGenerator : IIncrementalGenerator
{
    private const string AttributeName = "MBW.Generators.NonTryMethods.Abstracts.Attributes.GenerateNonTryMethodAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<EligibleClassContainer> classes = context.SyntaxProvider
            .ForAttributeWithMetadataName(AttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GetEligibleClass(ctx))
            .Where(static c => c != null)!;

        context.RegisterSourceOutput(classes, static (spc, @class) => Generate(spc, @class));
    }

    private static EligibleClassContainer? GetEligibleClass(GeneratorAttributeSyntaxContext context)
    {
        ClassDeclarationSyntax cds = (ClassDeclarationSyntax)context.TargetNode;

        if (!cds.Modifiers.Any(SyntaxKind.StaticKeyword))
            return null;

        CompilationUnitSyntax root = cds.SyntaxTree.GetCompilationUnitRoot();
        NamespaceDeclarationSyntax @namespace = cds.GetParentOfType<NamespaceDeclarationSyntax>();

        EligibleClassContainer eligibleClass = new EligibleClassContainer
        {
            Usings = root.Usings,
            Class = cds,
            Namespace = @namespace
        };

        foreach (MethodDeclarationSyntax method in cds.Members.OfType<MethodDeclarationSyntax>())
        {
            string methodName = method.Identifier.Text;
            bool isBoolReturn = method.ReturnType is PredefinedTypeSyntax asPredefined && asPredefined.Keyword.IsKind(SyntaxKind.BoolKeyword);

            SeparatedSyntaxList<ParameterSyntax> @params = method.ParameterList.Parameters;
            bool hasThisParam = @params.First().Modifiers.Any(SyntaxKind.ThisKeyword);

            if (methodName.StartsWith("Try") && isBoolReturn && hasThisParam)
                eligibleClass.Methods.Add(method);
        }

        return eligibleClass.Methods.Count > 0 ? eligibleClass : null;
    }

    private static void Generate(SourceProductionContext context, EligibleClassContainer @class)
    {
        string newClassName = $"{@class.Class.Identifier.ValueText}_AutogenNonTry";

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("using System;");
        foreach (UsingDirectiveSyntax usingDirectiveSyntax in @class.Usings)
            sb.Append(usingDirectiveSyntax.ToFullString());

        sb.Append("namespace ").AppendLine(@class.Namespace.Name.ToString());
        sb.AppendLine("{");
        sb.Append("    ")
            .Append(@class.Class.Modifiers.ToString())
            .AppendLine(" class ")
            .AppendLine(newClassName);
        sb.AppendLine("    {");

        foreach (MethodDeclarationSyntax method in @class.Methods)
        {
            SeparatedSyntaxList<ParameterSyntax> @params = method.ParameterList.Parameters;

            ParameterSyntax thisParam = @params.First();
            bool hasSingleOutParam = @params.Take(@params.Count - 1).All(x => !x.Modifiers.Any(SyntaxKind.OutKeyword)) &&
                                     @params.Last().Modifiers.Any(SyntaxKind.OutKeyword);

            ParameterSyntax[] arguments;
            if (hasSingleOutParam)
                arguments = @params.Skip(1).Take(@params.Count - 2).ToArray();
            else
                arguments = @params.Skip(1).ToArray();

            string argumentsCommaStr = arguments.Any() ? ", " : string.Empty;
            string argumentsStr = string.Join(", ", arguments.Select(s => s.ToFullString()));
            string argumentsNamesStr = string.Join(", ", arguments.Select(s => s.Identifier.Text));

            string outType;
            if (hasSingleOutParam)
                outType = @params.Last().Type.ToString();
            else
                outType = "void";

            sb.Append("        ")
                .Append(method.Modifiers.ToString())
                .Append(" ")
                .Append(outType)
                .Append(" ")
                .Append(method.Identifier.Text.Substring("Try".Length))
                .Append("(").Append(thisParam.ToFullString()).Append(argumentsCommaStr).Append(argumentsStr).AppendLine(")");

            sb.AppendLine("        {");
            sb.AppendLine(
                $"            if (!{thisParam.Identifier.Text}.{method.Identifier.Text}({argumentsNamesStr}{argumentsCommaStr}out {outType} result))");
            sb.AppendLine("                throw new Exception();");
            sb.AppendLine();
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(newClassName, SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}

