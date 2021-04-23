using System.Collections.Generic;
using System.Linq;
using MBW.Generators.NonTryMethods.Abstracts;
using MBW.Generators.NonTryMethods.Abstracts.Attributes;
using MBW.Generators.NonTryMethods.Helpers;
using MBW.Generators.NonTryMethods.Objects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.NonTryMethods
{
    class AutogenNonTryGeneratorSyntaxReceiver : ISyntaxReceiver
    {
        public IList<EligibleClassContainer> Classes { get; } = new List<EligibleClassContainer>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not ClassDeclarationSyntax cds ||
                !cds.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                !cds.HasAttribute(nameof(GenerateNonTryMethodAttribute)))
                return;

            CompilationUnitSyntax root = cds.SyntaxTree.GetCompilationUnitRoot();
            SyntaxList<UsingDirectiveSyntax> usings = root.Usings;

            NamespaceDeclarationSyntax @namespace = cds.GetParentOfType<NamespaceDeclarationSyntax>();

            EligibleClassContainer eligibleClass = new EligibleClassContainer
            {
                Usings = usings,
                Class = cds,
                Namespace = @namespace
            };
            Classes.Add(eligibleClass);

            foreach (MethodDeclarationSyntax method in cds.Members
                .OfType<MethodDeclarationSyntax>())
            {
                string methodName = method.Identifier.Text;
                bool isBoolReturn = method.ReturnType is PredefinedTypeSyntax asPredefined && asPredefined.Keyword.IsKind(SyntaxKind.BoolKeyword);

                SeparatedSyntaxList<ParameterSyntax> @params = method.ParameterList.Parameters;
                bool hasThisParam = @params.First().Modifiers.Any(SyntaxKind.ThisKeyword);

                if (methodName.StartsWith("Try") && isBoolReturn && hasThisParam)
                    eligibleClass.Methods.Add(method);
            }
        }
    }
}