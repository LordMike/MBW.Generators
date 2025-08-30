namespace MBW.Generators.GeneratorHelpers.Generator.Models;

internal readonly record struct TypeFqn(string[] NamespaceSegments, TypeSegment[] TypeSegments, string Normalized);