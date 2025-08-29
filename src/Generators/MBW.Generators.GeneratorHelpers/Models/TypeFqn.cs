namespace MBW.Generators.GeneratorHelpers.Models;

internal readonly record struct TypeFqn(string[] NamespaceSegments, TypeSegment[] TypeSegments, string Normalized);