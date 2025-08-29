using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.OverloadGenerator.Models;

sealed class MethodModel
{
    public MethodModel(IMethodSymbol method, ImmutableArray<Rule> rules)
    {
        Method = method;
        Rules = rules;
    }

    public IMethodSymbol Method { get; }
    public ImmutableArray<Rule> Rules { get; }
}