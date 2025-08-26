using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MBW.Generators.NonTryMethods.Models;

internal record struct TypeSourceAndDiagnostics(
    string? HintName,
    SourceText? Source,
    ImmutableArray<Diagnostic> Diagnostics);