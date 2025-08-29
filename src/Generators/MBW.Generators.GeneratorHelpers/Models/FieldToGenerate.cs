using Microsoft.CodeAnalysis;

namespace MBW.Generators.GeneratorHelpers.Models;

internal readonly record struct FieldToGenerate(
    FieldKind Kind,
    string MethodName,
    string[] NamespaceSegments,
    TypeSegment[]? TypeSegments,
    Location Location);