using Microsoft.CodeAnalysis;

namespace MBW.Generators.GeneratorHelpers.Models;

readonly record struct FieldToGenerate(
    FieldKind Kind,
    string MethodName,
    string[] NamespaceSegments,
    TypeSegment[]? TypeSegments,
    Location Location);