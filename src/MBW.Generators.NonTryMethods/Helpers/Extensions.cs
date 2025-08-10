using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MBW.Generators.NonTryMethods.Helpers;

static class Extensions
{
    public static TKind GetParentOfType<TKind>(this SyntaxNode item) where TKind : class
    {
        while (item.Parent != null)
        {
            item = item.Parent;

            if (item is TKind asKind)
                return asKind;
        }

        return default;
    }

    public static bool HasAttribute(this ClassDeclarationSyntax @class, string name)
    {
        if (name.EndsWith(nameof(Attribute)))
            name = name.Substring(0, name.Length - nameof(Attribute).Length);

        foreach (AttributeListSyntax attributeList in @class.AttributeLists)
        {
            foreach (AttributeSyntax listAttribute in attributeList.Attributes)
            {
                string attributeName = listAttribute.Name.TryGetInferredMemberName();
                if (attributeName == name)
                    return true;
            }
        }

        return false;
    }

}