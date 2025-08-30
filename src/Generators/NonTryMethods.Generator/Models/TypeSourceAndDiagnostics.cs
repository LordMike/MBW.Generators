using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MBW.Generators.NonTryMethods.Generator.Models;

internal record struct TypeSourceAndDiagnostics(
    string? HintName,
    SourceText? Source,
    ImmutableArray<Diagnostic> Diagnostics);