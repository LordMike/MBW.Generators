using Microsoft.CodeAnalysis;

namespace MBW.Generators.GeneratorHelpers.Generator.Models;

internal readonly record struct FieldToGenerate(
    FieldKind Kind,
    string MethodName,
    string[] NamespaceSegments,
    TypeSegment[]? TypeSegments,
    Location Location);