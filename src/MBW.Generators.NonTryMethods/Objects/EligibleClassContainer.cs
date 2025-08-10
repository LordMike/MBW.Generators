using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.NonTryMethods.Objects;

class EligibleClassContainer
{
    public SyntaxList<UsingDirectiveSyntax> Usings { get; set; }

    public NamespaceDeclarationSyntax Namespace { get; set; }

    public ClassDeclarationSyntax Class { get; set; }

    public IList<MethodDeclarationSyntax> Methods { get; } = new List<MethodDeclarationSyntax>();
}