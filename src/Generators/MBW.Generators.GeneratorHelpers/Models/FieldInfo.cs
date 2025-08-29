using Microsoft.CodeAnalysis;

namespace MBW.Generators.GeneratorHelpers.Models;

internal sealed class FieldInfo
{
    public FieldInfo(FieldKind kind, string methodBaseName, string[] namespaceSegments,
        TypeSegment[]? typeSegments, string normalizedTarget, Location location)
    {
        Kind = kind;
        MethodBaseName = methodBaseName;
        NamespaceSegments = namespaceSegments;
        TypeSegments = typeSegments;
        NormalizedTarget = normalizedTarget;
        Location = location;
        Generate = true;
    }

    public FieldKind Kind { get; }
    public string MethodBaseName { get; }
    public string? MethodName { get; set; }
    public string[] NamespaceSegments { get; }
    public TypeSegment[]? TypeSegments { get; }
    public string NormalizedTarget { get; }
    public Location Location { get; }
    public bool Generate { get; set; }
}